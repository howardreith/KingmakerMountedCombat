using System;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Chunk 6A: the transition-round adoption disposition, the transition ledger,
    // and the paired activation's mid-encounter adoption operation.
    internal static class Chunk6aCombatMountTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("real time needs no turn-slot disposition", RealTimeOwnership);
            runner.Run("an earlier partner roster slot retains its participation", EarlierPartnerSlotRetains);
            runner.Run("a later partner roster slot is prepared exactly once", LaterPartnerSlotPrepares);
            runner.Run("an unresolvable partner slot refuses the transition", UnresolvablePartnerSlotRefuses);
            runner.Run("adoption refuses a turn that is not the exact rider's", WrongTurnRefusesAdoption);
            runner.Run("adoption disposition reasons name the exact obstacle", AdoptionRefusalReasons);
            runner.Run("adoption grants the principal without repeating preparation", AdoptionDoesNotPrepareThePrincipal);
            runner.Run("adoption ends a spent partner slot instead of reopening it", AdoptionEndsSpentPartnerSlot);
            runner.Run("every shape of prior partner expenditure keeps its exact debt", SpentPartnerVariantsKeepTheirDebt);
            runner.Run("a skipped partner slot never gains participation", SkippedPartnerSlotsNeverGainParticipation);
            runner.Run("a spent partner becomes eligible only at its next allocation", SpentPartnerRecoversOnlyNextAllocation);
            runner.Run("adoption reserves one partner preparation when its slot is pending", AdoptionReservesPartnerPreparation);
            runner.Run("the adoption plan refuses an unavailable or malformed disposition", AdoptionPlanRefusesMalformedInput);
            runner.Run("the adoption plan matches only an identical observation", AdoptionPlanMatchesOnlyItself);
            runner.Run("the adoption plan names the exact change it observed", AdoptionPlanNamesTheChange);
            runner.Run("the adoption plan binds exactly the committed generation", AdoptionPlanBindsCommittedGeneration);
            runner.Run("a compensated combat mount leaves no relationship residue", CompensatedCombatMountLeavesNoResidue);
            runner.Run("a preparing rider turn refuses the transition with its own reason", PreparingRiderTurnRefusesTheTransition);
            runner.Run("a dismount delivery rejects every wrong target identity", DismountTargetIdentityRejectsWrongConditions);
            runner.Run("the ledger's in-flight window is exactly admit to settle", LedgerInFlightWindowIsExact);
            runner.Run("combat Mount requires the qualified paired authority", CombatMountRequiresQualifiedAuthority);
            runner.Run("an authority withdrawn after admission keeps the committed cost and forms no relationship", AuthorityWithdrawnAfterAdmissionKeepsTheCost);
            runner.Run("a mounted rider keeps its Dismount after the feature or policy is disabled", DismountSurvivesFeatureDisable);
            runner.Run("adoption refuses a used, split, suspended or repeated boundary", AdoptionRefusesUnusableBoundary);
            runner.Run("an adopted activation finalizes and begins the next round normally", AdoptedActivationFinalizes);
            runner.Run("adoption preserves every observed actor debt", AdoptionPreservesDebt);
            runner.Run("the ledger makes a repeated delivery of one control idempotent", LedgerSuppressesDuplicateControl);
            runner.Run("the ledger refuses a second voluntary transition in flight", LedgerRefusesConcurrentTransition);
            runner.Run("the ledger separates voluntary cost from forced cleanup", LedgerSeparatesForcedDetach);
            runner.Run("repeated forced cleanup for one generation is recorded once", LedgerForcedDetachIsIdempotent);
            runner.Run("the ledger requires an exact native control identity", LedgerRequiresControlIdentity);
            runner.Run("the ledger refuses to admit forced detach as voluntary", LedgerRefusesForcedAsVoluntary);
            runner.Run("the ledger retains a bounded history without evicting the in-flight record", LedgerRetentionIsBounded);
            runner.Run("the disposition table is total over the roster positions", DispositionTableIsTotal);
            runner.Run("a split adopted pair conserves debt and refuses further grants", AdoptedSplitConservesDebt);
            runner.Run("combat Mount refusal feedback names its exact obstacle", CombatMountRefusalFeedback);
            runner.Run("combat Dismount gates combine without masking each other", CombatDismountGatesCombine);
            runner.Run("relationship cleanup stays idempotent after a voluntary combat mount", VoluntaryCombatCleanupIsIdempotent);
        }

        private static void LedgerRetentionIsBounded()
        {
            var ledger = new MountedTransitionLedger();
            MountedTransitionRecord record;
            string refusal;
            for (var index = 0; index < 40; index++)
            {
                var identity = "shell:" + index.ToString(System.Globalization.CultureInfo.InvariantCulture);
                TestRunner.True(ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, identity,
                        "rider", "mount", index, out record, out refusal),
                    "Admission " + index + " was refused: " + refusal);
                TestRunner.True(ledger.HasVoluntaryTransitionInFlight,
                    "Retention trimming evicted the in-flight record at " + index + ".");
                TestRunner.True(ledger.Find(identity) != null,
                    "The in-flight record was not findable at " + index + ".");
                ledger.Settle(record, true);
            }
            TestRunner.True(ledger.Records.Count <= 24, "Retention exceeded its bound: " + ledger.Records.Count);
            TestRunner.Equal(40L, ledger.AcceptedMountCount, "Accepted transitions were lost with the trimmed history.");
            TestRunner.Equal(null, ledger.Find("shell:0"), "The oldest record was not trimmed.");
            TestRunner.True(ledger.Find("shell:39") != null, "The newest record was trimmed.");
        }

        private static void DispositionTableIsTotal()
        {
            // Every roster position relative to the running principal resolves to
            // exactly one disposition, and only a strictly later unskipped slot is
            // ever prepared.
            for (var mountSlot = -1; mountSlot <= 8; mountSlot++)
            {
                var resolved = Resolve(mountSlot);
                var expected =
                    mountSlot < 0 || mountSlot == RiderSlot ? MidEncounterAdoption.Unavailable :
                    mountSlot < RiderSlot ? MidEncounterAdoption.RetainPartnerParticipation :
                    MidEncounterAdoption.PreparePartnerThisRound;
                TestRunner.Equal(expected, resolved, "Roster position " + mountSlot + " resolved to " + resolved + ".");
                if (resolved == MidEncounterAdoption.PreparePartnerThisRound)
                {
                    TestRunner.True(mountSlot > RiderSlot,
                        "A partner at or before the principal's slot was prepared: " + mountSlot);
                }
            }
        }

        private static void AdoptedSplitConservesDebt()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            pair.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound);
            pair.BeginActorPreparation(mount, turn);
            pair.FinishActorPreparation(mount);
            pair.State(rider).Observe(0f, 3f, 0f);
            pair.State(mount).Observe(6f, 3f, 0f);
            pair.Detach();
            TestRunner.True(pair.Split, "Detach did not split the adopted activation.");
            TestRunner.Equal(3f, pair.State(rider).MoveSpent, "Split lost the adopted rider's Move debt.");
            TestRunner.Equal(6f, pair.State(mount).StandardSpent, "Split lost the adopted mount's Standard debt.");
            TestRunner.True(!pair.CanAddress(mount, turn), "A split adopted pair could still address its partner.");
            TestRunner.True(!pair.OwnsRoundEffects(mount, turn), "A split adopted pair still owned partner round effects.");
            TestRunner.True(!pair.Begin(turn), "A split adopted pair granted another activation on its own boundary.");
            TestRunner.True(!pair.AdoptRunningBoundary(new object(), MidEncounterAdoption.PreparePartnerThisRound),
                "A split adopted pair adopted a second boundary.");
        }

        private static void CombatMountRefusalFeedback()
        {
            // Each combat gate must surface its own obstacle, never a generic one.
            var noTurn = EligibleCombatContext();
            noTurn.CombatTurnEligible = false;
            TestRunner.True(Reasons(noTurn).Contains("current turn"), "The turn obstacle is not named.");
            var noAdjacency = EligibleCombatContext();
            noAdjacency.PairAdjacent = false;
            TestRunner.True(Reasons(noAdjacency).Contains("adjacent"), "The adjacency obstacle is not named.");
            var noMove = EligibleCombatContext();
            noMove.RiderHasMoveAction = false;
            TestRunner.True(Reasons(noMove).Contains("no Move action"), "The Move obstacle is not named.");
            var noAdoption = EligibleCombatContext();
            noAdoption.PairedAdoptionAvailable = false;
            noAdoption.PairedAdoptionUnavailableReason = "exact adoption obstacle";
            TestRunner.True(Reasons(noAdoption).Contains("exact adoption obstacle"),
                "The adoption obstacle is replaced by a generic reason.");
            var noAdoptionReason = EligibleCombatContext();
            noAdoptionReason.PairedAdoptionAvailable = false;
            TestRunner.True(Reasons(noAdoptionReason).Contains("take over this encounter"),
                "A missing adoption reason produced no fallback obstacle.");
            // An eligible combat context must produce no reason at all, so none of
            // the gates above is firing by accident.
            TestRunner.Equal(string.Empty, Reasons(EligibleCombatContext()).Trim(),
                "An eligible combat Mount produced a reason.");
        }

        private static void CombatDismountGatesCombine()
        {
            var context = EligibleCombatContext();
            context.RelationshipState = RelationshipState.Mounted;
            context.CombatTurnEligible = false;
            context.RiderHasMoveAction = false;
            context.RelationshipTransitionInFlight = true;
            var reasons = Reasons(context);
            TestRunner.True(reasons.Contains("already in flight") && reasons.Contains("rider-led current turn") &&
                    reasons.Contains("no Move action"),
                "Combined Dismount gates masked one another: " + reasons);
            // The committed native shell must clear only the Move predicate.
            context.NativeMoveActionShellAdmitted = true;
            var admitted = Reasons(context);
            TestRunner.True(!admitted.Contains("no Move action") && admitted.Contains("rider-led current turn") &&
                    admitted.Contains("already in flight"),
                "The admitted shell cleared more than the stale Move predicate: " + admitted);
        }

        private static void VoluntaryCombatCleanupIsIdempotent()
        {
            var candidate = CombatCandidate();
            var runtime = new CountingRuntime();
            var coordinator = new MountedRelationshipCoordinator(runtime);
            TestRunner.True(coordinator.Mount(candidate, MountedRelationshipAdmission.VoluntaryCombat).Succeeded,
                "Voluntary combat mount was refused.");
            var first = coordinator.Dismount(CleanupTrigger.Manual);
            TestRunner.True(first.Succeeded && first.Trigger == CleanupTrigger.Manual,
                "Voluntary combat dismount did not complete as Manual.");
            var second = coordinator.Dismount(CleanupTrigger.Exception);
            TestRunner.True(second.Succeeded, "Repeated cleanup after a voluntary combat mount failed.");
            TestRunner.True(!second.MovementAuthorityResidual && !second.PresentationResidual,
                "Repeated cleanup retained residue.");
            TestRunner.Equal(1, runtime.Acquires, "Voluntary combat mount acquired movement authority more than once.");
            TestRunner.Equal(1, runtime.Attaches, "Voluntary combat mount attached presentation more than once.");
            TestRunner.Equal(1, runtime.Restores, "Repeated cleanup restored movement authority more than once.");
            TestRunner.Equal(1, runtime.Detaches, "Repeated cleanup restored presentation more than once.");
        }

        // R2. The plan is a decision about an exact live encounter, so it refuses to
        // record an unavailable disposition or a malformed pair at construction.
        private static void AdoptionPlanRefusesMalformedInput()
        {
            TestRunner.True(Threw(() => Plan(MidEncounterAdoption.Unavailable, 7L)),
                "An unavailable disposition was recorded as a plan.");
            TestRunner.True(Threw(() => new MidEncounterAdoptionPlan(
                    MidEncounterAdoption.PreparePartnerThisRound, 7L, "area", "rider-1", "rider-1",
                    true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true)),
                "A plan was built for one actor playing both roles.");
            TestRunner.True(Threw(() => new MidEncounterAdoptionPlan(
                    MidEncounterAdoption.PreparePartnerThisRound, 7L, "area", "rider-1", "  ",
                    true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true)),
                "A plan was built without an exact companion identity.");
            var live = Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L);
            TestRunner.True(live.EncounterStillLive, "A live encounter was recorded as ended.");
            var ended = new MidEncounterAdoptionPlan(
                MidEncounterAdoption.PreparePartnerThisRound, 7L, "area", "rider-1", "mount-1",
                true, 2, "rider-1", 3, 4, false, false, true,
                false, false, false, true, true, true, true);
            TestRunner.True(!ended.EncounterStillLive, "An ended encounter was recorded as live.");
        }

        // R2. Revalidation is exact equality over every observed fact.
        private static void AdoptionPlanMatchesOnlyItself()
        {
            var plan = Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L);
            TestRunner.True(plan.Matches(Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L)),
                "An identical observation did not match its own plan.");
            TestRunner.True(!plan.Matches(null), "A missing observation matched a plan.");
            TestRunner.True(!plan.Matches(Plan(MidEncounterAdoption.RetainPartnerParticipation, 7L)),
                "A changed disposition still matched.");
            TestRunner.True(!plan.Matches(Plan(MidEncounterAdoption.PreparePartnerThisRound, 8L)),
                "A changed relationship generation still matched.");
            foreach (var mutated in MutatedPlans())
            {
                TestRunner.True(!plan.Matches(mutated.Value),
                    "A plan with a changed " + mutated.Key + " still matched.");
            }
        }

        // R2. A refusal must say what actually changed, not a generic message.
        private static void AdoptionPlanNamesTheChange()
        {
            var plan = Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L);
            TestRunner.Equal(null, plan.DescribeDifference(Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L)),
                "An identical observation reported a difference.");
            TestRunner.True(plan.DescribeDifference(null).Contains("could not be observed"),
                "A missing observation was not named.");
            var expected = new System.Collections.Generic.Dictionary<string, string>
            {
                { "round", "round advanced" },
                { "currentTurn", "current turn moved" },
                { "rosterIndex", "initiative order changed" },
                { "surprise", "surprise or visibility" },
                { "liveness", "liveness changed" },
                { "ableToAct", "ability to act changed" },
                { "conscious", "consciousness changed" },
                { "session", "encounter session changed" },
                { "identity", "rider or companion identity changed" },
                { "mode", "combat mode changed" }
            };
            foreach (var mutated in MutatedPlans())
            {
                var described = plan.DescribeDifference(mutated.Value);
                TestRunner.True(described != null, "A changed " + mutated.Key + " produced no description.");
                TestRunner.True(described.Contains(expected[mutated.Key]),
                    "A changed " + mutated.Key + " was described as: " + described);
            }
        }

        // R2. The commit's generation check is exact equality, so the plan is
        // rebound to the one generation a committed relationship produces.
        private static void AdoptionPlanBindsCommittedGeneration()
        {
            var plan = Plan(MidEncounterAdoption.PreparePartnerThisRound, 7L);
            var committed = plan.WithCommittedGeneration(8L);
            TestRunner.Equal(8L, committed.RelationshipGeneration,
                "The committed plan did not take the committed generation.");
            TestRunner.True(!plan.Matches(committed), "The committed plan matched the pre-commit plan.");
            TestRunner.True(committed.Matches(Plan(MidEncounterAdoption.PreparePartnerThisRound, 8L)),
                "Rebinding the generation changed another observed fact.");
            TestRunner.True(Threw(() => plan.WithCommittedGeneration(7L)),
                "A generation that did not advance was accepted as committed.");
            TestRunner.True(Threw(() => plan.WithCommittedGeneration(9L)),
                "A generation that advanced by two was accepted as committed.");
            TestRunner.True(Threw(() => plan.WithCommittedGeneration(6L)),
                "A rewound generation was accepted as committed.");
        }

        // R2. When adoption is refused after the relationship attached, the
        // attachment is undone exactly: back to Unmounted, no residue, one restore
        // of each authority, and a repeated cleanup stays idempotent.
        private static void CompensatedCombatMountLeavesNoResidue()
        {
            var runtime = new CountingRuntime();
            var coordinator = new MountedRelationshipCoordinator(runtime);
            TestRunner.True(coordinator.Mount(CombatCandidate(), MountedRelationshipAdmission.VoluntaryCombat).Succeeded,
                "Voluntary combat mount was refused before the compensation could be tested.");
            TestRunner.Equal(RelationshipState.Mounted, coordinator.State,
                "The relationship did not attach before compensation.");
            var compensation = coordinator.Dismount(CleanupTrigger.AdoptionRefused);
            TestRunner.True(compensation.Succeeded, "Compensating cleanup failed.");
            TestRunner.Equal(CleanupTrigger.AdoptionRefused, compensation.Trigger,
                "Compensating cleanup did not record the adoption refusal as its trigger.");
            TestRunner.Equal(RelationshipState.Unmounted, coordinator.State,
                "The relationship did not return to unmounted after compensation.");
            TestRunner.True(!compensation.MovementAuthorityResidual && !compensation.PresentationResidual,
                "Compensating cleanup retained residue.");
            TestRunner.Equal(1, runtime.Restores, "Compensation restored movement authority more than once.");
            TestRunner.Equal(1, runtime.Detaches, "Compensation restored presentation more than once.");
            TestRunner.True(coordinator.ActivePair == null, "Compensation left the pair behind.");
            var again = coordinator.Dismount(CleanupTrigger.Exception);
            TestRunner.True(again.Succeeded && !again.MovementAuthorityResidual && !again.PresentationResidual,
                "Repeated cleanup after compensation was not idempotent.");
            TestRunner.Equal(1, runtime.Restores, "Repeated cleanup restored movement authority a second time.");
            // A fresh mount is admissible afterwards: compensation leaves no lock.
            TestRunner.True(coordinator.Mount(CombatCandidate(), MountedRelationshipAdmission.VoluntaryCombat).Succeeded,
                "A compensated relationship refused a later lawful mount.");
        }

        // R3. Turn-based delivery requires the rider's turn to be ACTING. The
        // Preparing boundary is refused, and with its own reason.
        private static void PreparingRiderTurnRefusesTheTransition()
        {
            TestRunner.True(CombatMountDismountPolicy.IsTurnEligible(true, true, true),
                "An acting rider turn was refused.");
            TestRunner.True(!CombatMountDismountPolicy.IsTurnEligible(true, true, false),
                "A rider turn that is not acting was admitted.");
            TestRunner.True(!CombatMountDismountPolicy.IsTurnEligible(true, false, true),
                "Another actor's acting turn was admitted.");
            TestRunner.True(CombatMountDismountPolicy.IsTurnEligible(false, false, false),
                "Real time was made to depend on a turn slot.");

            TestRunner.Equal(null,
                CombatMountDismountPolicy.DescribeTurnIneligibility("Mount Companion", true, true, false, true),
                "An eligible acting turn produced a refusal reason.");
            TestRunner.Equal(null,
                CombatMountDismountPolicy.DescribeTurnIneligibility("Mount Companion", false, false, false, false),
                "Real time produced a turn-based refusal reason.");
            var preparing = CombatMountDismountPolicy.DescribeTurnIneligibility(
                "Mount Companion", true, true, true, false);
            TestRunner.True(preparing != null && preparing.Contains("finished preparing"),
                "The preparing boundary was not named: " + preparing);
            var wrongActor = CombatMountDismountPolicy.DescribeTurnIneligibility(
                "Dismount", true, false, false, true);
            TestRunner.True(wrongActor != null && wrongActor.Contains("rider's current turn"),
                "A foreign current turn was not named: " + wrongActor);
            var idle = CombatMountDismountPolicy.DescribeTurnIneligibility(
                "Dismount", true, true, false, false);
            TestRunner.True(idle != null && idle.Contains("acting"),
                "A rider turn that is neither preparing nor acting was not named: " + idle);

            // Availability surfaces the exact reason for both actions.
            var mount = EligibleCombatContext();
            mount.CombatTurnEligible = false;
            mount.CombatTurnIneligibilityReason = preparing;
            TestRunner.True(Reasons(mount).Contains("finished preparing"),
                "Mount availability replaced the preparing reason with a generic one.");
            var dismount = EligibleCombatContext();
            dismount.RelationshipState = RelationshipState.Mounted;
            dismount.CombatTurnEligible = false;
            dismount.CombatTurnIneligibilityReason = "Dismount waits until the rider's turn has finished preparing.";
            TestRunner.True(Reasons(dismount).Contains("finished preparing"),
                "Dismount availability replaced the preparing reason with a generic one.");
        }

        // R4. Each wrong dismount target identity is rejected on its own terms.
        private static void DismountTargetIdentityRejectsWrongConditions()
        {
            TestRunner.Equal(null,
                DismountTargetIdentityPolicy.Refuse(true, true, "rider-1", "rider-1", true, true, 7L, 7L),
                "An exact self-targeted dismount was refused.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(false, false, "rider-1", "rider-1", true, true, 7L, 7L)
                    .Contains("must target its own rider"),
                "A null dismount target was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, false, "rider-1", "rider-1", true, true, 7L, 7L)
                    .Contains("must target its own rider"),
                "A foreign dismount target was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, "rider-2", "rider-1", true, true, 7L, 7L)
                    .Contains("created for a different rider"),
                "A dismount whose captured target changed was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, null, "rider-1", true, true, 7L, 7L)
                    .Contains("created for a different rider"),
                "A dismount with no captured target was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, "rider-1", null, true, true, 7L, 7L)
                    .Contains("created for a different rider"),
                "A dismount with no caster identity was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, "rider-1", "rider-1", false, false, 7L, 7L)
                    .Contains("rider changed"),
                "A dismount with no live rider was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, "rider-1", "rider-1", true, false, 7L, 7L)
                    .Contains("rider changed"),
                "A dismount whose relationship rider changed was admitted.");
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, true, "rider-1", "rider-1", true, true, 6L, 7L)
                    .Contains("relationship changed"),
                "A stale-generation dismount was admitted.");
            // The identity failures are reported before the generation one, so the
            // message always names the most specific obstacle.
            TestRunner.True(DismountTargetIdentityPolicy.Refuse(true, false, "rider-2", "rider-1", false, false, 6L, 7L)
                    .Contains("must target its own rider"),
                "A dismount with several faults did not name the most specific one.");
        }

        // R5. The save barrier's ledger term is exactly the admit-to-settle window,
        // for an accepted transition and for a refused one alike.
        private static void LedgerInFlightWindowIsExact()
        {
            foreach (var accepted in new[] { true, false })
            {
                var ledger = new MountedTransitionLedger();
                TestRunner.True(!ledger.HasVoluntaryTransitionInFlight,
                    "A fresh ledger reported a transition in flight.");
                MountedTransitionRecord record;
                string refusal;
                TestRunner.True(ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, "shell:1",
                        "rider-1", "mount-1", 7L, out record, out refusal),
                    "Admission was refused: " + refusal);
                TestRunner.True(ledger.HasVoluntaryTransitionInFlight,
                    "An admitted transition was not reported in flight.");
                TestRunner.Equal("shell:1", ledger.InFlightControlIdentity,
                    "The in-flight control identity was not the admitted one.");
                ledger.Settle(record, accepted);
                TestRunner.True(!ledger.HasVoluntaryTransitionInFlight,
                    "A settled transition (accepted=" + accepted + ") was still reported in flight.");
                TestRunner.Equal(null, ledger.InFlightControlIdentity,
                    "A settled transition left an in-flight control identity behind.");
                // Forced cleanup never opens the window.
                ledger.RecordForcedDetach("rider-1", "mount-1", 8L, "Death");
                TestRunner.True(!ledger.HasVoluntaryTransitionInFlight,
                    "Forced cleanup opened the voluntary in-flight window.");
            }
        }

        // Combat Mount is supported only on the accepted architecture, and the one typed
        // policy that prediction asks is the one execution-time admission asks.
        private static void CombatMountRequiresQualifiedAuthority()
        {
            // The complete truth table: only paired activation alone qualifies.
            for (var mask = 0; mask < 8; mask++)
            {
                var paired = (mask & 1) != 0;
                var unified = (mask & 2) != 0;
                var scheduler = (mask & 4) != 0;
                var expected = paired && !unified && !scheduler;
                TestRunner.Equal(expected,
                    MountedAuthorityPolicy.IsQualifiedForCombatMount(paired, unified, scheduler),
                    "Authority qualification wrong for paired=" + paired + " unified=" + unified + " scheduler=" + scheduler);
                var reason = MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(paired, unified, scheduler);
                TestRunner.Equal(expected, reason == null,
                    "Reason presence disagreed with qualification for paired=" + paired +
                    " unified=" + unified + " scheduler=" + scheduler);
            }
            // Each obstacle names itself.
            TestRunner.True(MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(false, false, false)
                    .Contains("paired activation to be enabled"),
                "A disabled paired activation was not named.");
            TestRunner.True(MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(true, true, false)
                    .Contains("unified mounted turn"),
                "The retired unified mounted turn was not named.");
            TestRunner.True(MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(true, false, true)
                    .Contains("paired command scheduler"),
                "The retired paired command scheduler was not named.");
            TestRunner.True(MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(true, true, true)
                    .Contains("both retired turn experiments"),
                "Two retired authorities were not named together.");

            // Prediction refuses in combat with the exact obstacle, and never outside it.
            var inCombat = EligibleCombatContext();
            inCombat.CombatMountAuthorityQualified = false;
            inCombat.CombatMountAuthorityReason = "Mounting during combat requires paired activation to be enabled.";
            TestRunner.True(Reasons(inCombat).Contains("paired activation to be enabled"),
                "Combat Mount prediction did not surface the authority obstacle.");
            var fallback = EligibleCombatContext();
            fallback.CombatMountAuthorityQualified = false;
            TestRunner.True(Reasons(fallback).Contains("qualified paired authority"),
                "A missing authority reason produced no fallback obstacle.");
            var outOfCombat = EligibleCombatContext();
            outOfCombat.InCombat = false;
            outOfCombat.CombatMountAuthorityQualified = false;
            outOfCombat.CombatMountAuthorityReason = "should not appear";
            TestRunner.True(!Reasons(outOfCombat).Contains("should not appear"),
                "Mounting outside combat was gated on the paired authority.");
        }

        // AUTHORITY TIMING. The qualified-authority decision is asked twice, at two very
        // different moments, and only the first one can prevent a cost. Kingmaker owns
        // the Move; KMC observes it and never rewrites it, so a late refusal is a spent
        // Move with no mount -- not a free retry. Both halves are asserted here so the
        // documented language cannot drift away from the behaviour.
        private static void AuthorityWithdrawnAfterAdmissionKeepsTheCost()
        {
            var ledger = new MountedTransitionLedger();

            // BEFORE. Unqualified at prediction: the ability is refused while the player
            // is still looking at it, so Kingmaker builds no command. Nothing is admitted,
            // nothing is committed, nothing changes.
            var predicted = EligibleCombatContext();
            predicted.InCombat = true;
            predicted.CombatMountAuthorityQualified = false;
            predicted.CombatMountAuthorityReason =
                MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(false, false, false);
            var prediction = MountedPlayerActionEvaluator.Evaluate(predicted);
            TestRunner.True(!prediction.IsEnabled,
                "An unqualified authority was predicted as an available combat Mount.");
            TestRunner.True(prediction.Feedback.Contains("paired activation"),
                "The prediction refusal did not name the authority obstacle: " + prediction.Feedback);
            TestRunner.Equal(0L, ledger.AdmittedMountCount,
                "A refusal at prediction admitted a transition.");
            TestRunner.Equal(0L, ledger.RefusedVoluntaryCount,
                "A refusal at prediction booked a voluntary transition it never made.");

            // AFTER. Qualified at prediction, so Kingmaker created and committed the Move
            // command; the settings then changed before delivery reached the relationship
            // service. The committed record already exists at this point.
            MountedTransitionRecord record;
            string admissionRefusal;
            TestRunner.True(ledger.TryAdmitVoluntary(
                    MountedTransitionKind.VoluntaryMount, "native:1:mount",
                    "rider-1", "mount-1", 7L, out record, out admissionRefusal),
                "A lawful combat Mount admission was refused: " + admissionRefusal);
            TestRunner.Equal(1L, ledger.AdmittedMountCount, "The committed transition was not recorded.");

            var lateRefusal = MountedAuthorityPolicy.DescribeUnqualifiedCombatMount(true, true, false);
            TestRunner.True(!MountedAuthorityPolicy.IsQualifiedForCombatMount(true, true, false) &&
                lateRefusal != null && lateRefusal.Contains("unified mounted turn"),
                "A retired authority switched on after admission was treated as qualified.");

            // The execution gate refuses. The record settles as refused and STAYS on the
            // ledger: the Move Kingmaker already charged is not unmade.
            ledger.Settle(record, false);
            TestRunner.True(record.Settled && !record.Accepted,
                "A refused delivery left its record unsettled or accepted.");
            TestRunner.Equal(1L, ledger.AdmittedMountCount,
                "The late refusal erased the committed transition.");
            TestRunner.Equal(0L, ledger.AcceptedMountCount,
                "A refused transition was counted as an accepted mount.");
            TestRunner.Equal(1L, ledger.RefusedVoluntaryCount,
                "The late refusal was not booked as a refused voluntary transition.");
            TestRunner.True(!ledger.HasVoluntaryTransitionInFlight,
                "The refused transition stayed in flight.");
            TestRunner.Equal(7L, record.GenerationBefore,
                "The refused transition rewrote the relationship generation it observed.");
            // No compensating cleanup is booked to undo a committed cost: forced detach is
            // cleanup for a real pair, never a refund instrument.
            TestRunner.Equal(0L, ledger.ForcedDetachCount,
                "The late refusal booked a cleanup in order to undo a committed cost.");

            // TERMINAL. That exact native control can never try again on a later frame.
            MountedTransitionRecord replay;
            string replayRefusal;
            TestRunner.True(!ledger.TryAdmitVoluntary(
                    MountedTransitionKind.VoluntaryMount, "native:1:mount",
                    "rider-1", "mount-1", 7L, out replay, out replayRefusal),
                "The refused native control identity was admitted a second time.");
            TestRunner.True(replayRefusal != null && replayRefusal.Contains("already been delivered"),
                "A replayed control identity was refused for the wrong reason: " + replayRefusal);
            TestRunner.Equal(1L, ledger.DuplicateControlSuppressedCount,
                "The replayed control identity was not recorded as a suppressed duplicate.");

            // A NEW lawful control, once the authority is qualified again, is admitted
            // normally: the refusal was terminal for that shell, not for the pair.
            TestRunner.True(MountedAuthorityPolicy.IsQualifiedForCombatMount(true, false, false),
                "The accepted architecture was reported as unqualified.");
            MountedTransitionRecord fresh;
            string freshRefusal;
            TestRunner.True(ledger.TryAdmitVoluntary(
                    MountedTransitionKind.VoluntaryMount, "native:2:mount",
                    "rider-1", "mount-1", 7L, out fresh, out freshRefusal),
                "A fresh lawful control was refused after an earlier terminal refusal: " + freshRefusal);
            ledger.Settle(fresh, true);
            TestRunner.Equal(1L, ledger.AcceptedMountCount,
                "The fresh lawful transition was not accepted.");
            TestRunner.Equal(2L, ledger.AdmittedMountCount,
                "The ledger lost one of the two committed transitions.");
        }

        // The escape hatch. A mounted or faulted rider must never be stranded when the
        // movement feature or the paired policy is switched off.
        private static void DismountSurvivesFeatureDisable()
        {
            foreach (var faulted in new[] { false, true })
            {
                TestRunner.True(NativeMountedControlPolicy.ShouldLease(
                        NativeMountedControlKind.Dismount, false, false, true, !faulted, faulted, true, false),
                    "Dismount was withdrawn from the exact rider with the feature disabled (faulted=" + faulted + ").");
                TestRunner.True(NativeMountedControlPolicy.ShouldLease(
                        NativeMountedControlKind.Dismount, false, true, true, !faulted, faulted, true, false),
                    "Dismount was withdrawn from the exact rider with a retired authority live (faulted=" + faulted + ").");
                // Only the exact rider keeps it; the mount never gains it.
                TestRunner.True(!NativeMountedControlPolicy.ShouldLease(
                        NativeMountedControlKind.Dismount, false, false, true, !faulted, faulted, false, true),
                    "The mount was leased a Dismount control (faulted=" + faulted + ").");
                // Mount stays feature-gated.
                TestRunner.True(!NativeMountedControlPolicy.ShouldLease(
                        NativeMountedControlKind.MountCompanion, false, false, true, !faulted, faulted, true, false),
                    "Mount survived the disabled feature (faulted=" + faulted + ").");
            }
            // Unmounted and feature-disabled: nothing is leased at all.
            TestRunner.True(!NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.Dismount, false, false, true, false, false, true, false),
                "An unmounted rider was leased a Dismount control with the feature disabled.");
            TestRunner.True(!NativeMountedControlPolicy.ShouldLease(
                    NativeMountedControlKind.MountCompanion, false, false, true, false, false, true, false),
                "An unmounted rider was leased a Mount control with the feature disabled.");
            // The feature flag never appears in the mounted Dismount availability branch,
            // so the action stays visible and enabled for the exact rider.
            var mounted = EligibleCombatContext();
            mounted.RelationshipState = RelationshipState.Mounted;
            mounted.FeatureEnabled = false;
            mounted.InCombat = false;
            var availability = MountedPlayerActionEvaluator.Evaluate(mounted);
            TestRunner.Equal(MountedPlayerActionKind.Dismount, availability.Action,
                "A mounted rider with the feature disabled was not offered Dismount.");
            TestRunner.True(availability.IsVisible && availability.IsEnabled,
                "Dismount was hidden or disabled for a mounted rider with the feature disabled: " + availability.Feedback);

            // DELIVERY. The combat delivery path admits through this same branch with the
            // native Move shell already committed, so a disabled feature must not refuse a
            // rider who has genuinely paid.
            var delivery = EligibleCombatContext();
            delivery.RelationshipState = RelationshipState.Mounted;
            delivery.FeatureEnabled = false;
            delivery.InCombat = true;
            delivery.CombatTurnEligible = true;
            delivery.RiderHasMoveAction = false;
            delivery.NativeMoveActionShellAdmitted = true;
            var deliveryAvailability = MountedPlayerActionEvaluator.Evaluate(delivery);
            TestRunner.True(deliveryAvailability.IsVisible && deliveryAvailability.IsEnabled,
                "A committed native Move shell was refused Dismount delivery with the feature disabled: " +
                deliveryAvailability.Feedback);

            // COST. The escape defeats the feature gate and nothing else. Without a Move
            // action and without a committed native shell the rider is still refused, and
            // the reason names the resource -- never the feature. An escape that also
            // waived the cost would be a free Dismount, which is a different defect.
            var unpaid = EligibleCombatContext();
            unpaid.RelationshipState = RelationshipState.Mounted;
            unpaid.FeatureEnabled = false;
            unpaid.InCombat = true;
            unpaid.CombatTurnEligible = true;
            unpaid.RiderHasMoveAction = false;
            unpaid.NativeMoveActionShellAdmitted = false;
            var unpaidAvailability = MountedPlayerActionEvaluator.Evaluate(unpaid);
            TestRunner.True(unpaidAvailability.IsVisible && !unpaidAvailability.IsEnabled,
                "The escape waived the rider's Move cost for Dismount.");
            TestRunner.True(unpaidAvailability.Feedback.Contains("no Move action") &&
                !unpaidAvailability.Feedback.Contains("private-alpha"),
                "An unpaid combat Dismount blamed the feature instead of the missing Move action: " +
                unpaidAvailability.Feedback);

            // TURN. The escape likewise does not hand the rider a turn it does not own.
            var wrongTurn = EligibleCombatContext();
            wrongTurn.RelationshipState = RelationshipState.Mounted;
            wrongTurn.FeatureEnabled = false;
            wrongTurn.InCombat = true;
            wrongTurn.CombatTurnEligible = false;
            wrongTurn.CombatTurnIneligibilityReason = "Dismount belongs to the rider's acting turn.";
            wrongTurn.RiderHasMoveAction = true;
            var wrongTurnAvailability = MountedPlayerActionEvaluator.Evaluate(wrongTurn);
            TestRunner.True(wrongTurnAvailability.IsVisible && !wrongTurnAvailability.IsEnabled &&
                wrongTurnAvailability.Feedback.Contains("acting turn"),
                "The escape granted a Dismount outside the rider's own turn: " + wrongTurnAvailability.Feedback);

            // TRANSITION. A second Dismount is still refused while one is in flight.
            var inFlight = EligibleCombatContext();
            inFlight.RelationshipState = RelationshipState.Mounted;
            inFlight.FeatureEnabled = false;
            inFlight.RelationshipTransitionInFlight = true;
            var inFlightAvailability = MountedPlayerActionEvaluator.Evaluate(inFlight);
            TestRunner.True(inFlightAvailability.IsVisible && !inFlightAvailability.IsEnabled &&
                inFlightAvailability.Feedback.Contains("already in flight"),
                "The escape admitted a concurrent Dismount: " + inFlightAvailability.Feedback);

            // TYPED POLICY. The one escape predicate is exact in its own right: only the
            // Dismount kind, only a live or faulted relationship, only the exact rider.
            TestRunner.True(NativeMountedControlPolicy.IsDismountEscape(
                    NativeMountedControlKind.Dismount, true, false, true),
                "The escape refused a mounted exact rider.");
            TestRunner.True(NativeMountedControlPolicy.IsDismountEscape(
                    NativeMountedControlKind.Dismount, false, true, true),
                "The escape refused a faulted exact rider.");
            TestRunner.True(!NativeMountedControlPolicy.IsDismountEscape(
                    NativeMountedControlKind.Dismount, false, false, true),
                "The escape admitted an unmounted rider.");
            TestRunner.True(!NativeMountedControlPolicy.IsDismountEscape(
                    NativeMountedControlKind.Dismount, true, false, false),
                "The escape admitted a unit that is not the rider.");
            foreach (var kind in new[]
            {
                NativeMountedControlKind.None,
                NativeMountedControlKind.MountCompanion,
                NativeMountedControlKind.RiderPrimary,
                NativeMountedControlKind.MountPrimary
            })
            {
                TestRunner.True(!NativeMountedControlPolicy.IsDismountEscape(kind, true, false, true),
                    "The escape widened past Dismount to " + kind + ".");
            }
        }

        private static MidEncounterAdoptionPlan Plan(MidEncounterAdoption disposition, long generation) =>
            new MidEncounterAdoptionPlan(disposition, generation, "area-1", "rider-1", "mount-1",
                true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true);

        // One mutated plan per observed fact, so revalidation coverage is total.
        private static System.Collections.Generic.Dictionary<string, MidEncounterAdoptionPlan> MutatedPlans() =>
            new System.Collections.Generic.Dictionary<string, MidEncounterAdoptionPlan>
            {
                { "mode", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", false, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true) },
                { "round", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 3, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true) },
                { "currentTurn", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "other-1", 3, 4, false, false, true, true, true, true, true, true, true, true) },
                { "rosterIndex", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 5, false, false, true, true, true, true, true, true, true, true) },
                { "surprise", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 4, true, false, true, true, true, true, true, true, true, true) },
                { "liveness", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 4, false, false, true, false, true, true, true, true, true, true) },
                { "ableToAct", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 4, false, false, true, true, true, true, false, true, true, true) },
                { "conscious", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, false, true) },
                { "session", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-2",
                    "rider-1", "mount-1", true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true) },
                { "identity", new MidEncounterAdoptionPlan(MidEncounterAdoption.PreparePartnerThisRound, 7L, "area-1",
                    "rider-1", "mount-2", true, 2, "rider-1", 3, 4, false, false, true, true, true, true, true, true, true, true) }
            };

        private static string Reasons(MountedPlayerActionContext context)
        {
            var result = MountedPlayerActionEvaluator.Evaluate(context);
            return string.Join(" ", System.Linq.Enumerable.ToArray(result.UnavailableReasons));
        }

        private static MountedPlayerActionContext EligibleCombatContext() => new MountedPlayerActionContext
        {
            RelationshipState = RelationshipState.Unmounted,
            GameAvailable = true,
            FeatureEnabled = true,
            ExactlyOneRiderSelected = true,
            RiderIsExactlyMedium = true,
            RiderBodyProfileSupported = true,
            ExactActiveOwnedSupportedMount = true,
            MountDisplayName = "Horse",
            MountIsStrictlyLarger = true,
            RiderIsAliveAndConscious = true,
            MountIsAliveAndConscious = true,
            RiderIsDirectlyControllableAndInGame = true,
            MountIsDirectlyControllableAndInGame = true,
            InCombat = true,
            CombatTurnEligible = true,
            RiderHasMoveAction = true,
            PairAdjacent = true,
            PairedAdoptionAvailable = true,
            SafeGameMode = true,
            ViewsAndStockAgentsAvailable = true,
            StockAgentsReady = true,
            AgentOverridesAvailable = true
        };

        private static MountedPairCandidate CombatCandidate() => new MountedPairCandidate("rider-1", "mount-1")
        {
            RiderIsDirectlyControllable = true,
            MountIsDirectlyControllable = true,
            RiderIsAliveAndConscious = true,
            MountIsAliveAndConscious = true,
            ExactReciprocalCompanionRelationship = true,
            PartyIsInCombat = true,
            RiderSizeOrdinal = 4,
            MountSizeOrdinal = 5,
            RiderViewAndStockAgentAvailable = true,
            MountViewAndStockAgentAvailable = true,
            RiderStockAgentEnabled = true,
            MountStockAgentEnabled = true,
            RiderAgentOverrideAvailable = true,
            MountAgentOverrideAvailable = true,
            RiderIsExactlyMedium = true,
            SafeMovementMode = true
        };

        private sealed class CountingRuntime : IMountedPairRuntime
        {
            internal int Acquires;
            internal int Attaches;
            internal int Restores;
            internal int Detaches;

            public void AcquireMovementAuthority(MountedPair pair) { Acquires++; }
            public void AttachPresentation(MountedPair pair) { Attaches++; }
            public void RestorePresentation(MountedPair pair) { Detaches++; }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger) { Restores++; }
        }

        private const int RiderSlot = 3;

        private static MidEncounterAdoption Resolve(int mountSlot) =>
            MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, mountSlot, false, false, true);

        private static void RealTimeOwnership()
        {
            TestRunner.Equal(MidEncounterAdoption.RealTimeOwnership,
                MidEncounterAdoptionPolicy.Resolve(false, false, -1, -1, true, true, false),
                "Real time demanded a turn-slot disposition.");
            TestRunner.Equal(null,
                MidEncounterAdoptionPolicy.DescribeUnavailable(false, false, -1, -1),
                "Real time produced a turn-based refusal reason.");
        }

        private static void EarlierPartnerSlotRetains()
        {
            TestRunner.Equal(MidEncounterAdoption.RetainPartnerParticipation, Resolve(0),
                "A partner whose slot already passed was prepared again.");
            TestRunner.Equal(MidEncounterAdoption.RetainPartnerParticipation, Resolve(RiderSlot - 1),
                "The slot immediately before the rider was treated as pending.");
            // A surprise-skipped earlier slot still cannot take another one this
            // round: the positional walk only moves forward from the principal.
            TestRunner.Equal(MidEncounterAdoption.RetainPartnerParticipation,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, 0, true, true, false),
                "An earlier surprise-skipped partner changed the disposition.");
        }

        private static void LaterPartnerSlotPrepares()
        {
            TestRunner.Equal(MidEncounterAdoption.PreparePartnerThisRound, Resolve(RiderSlot + 1),
                "A pending partner slot was not prepared.");
            TestRunner.Equal(MidEncounterAdoption.PreparePartnerThisRound, Resolve(RiderSlot + 9),
                "A far pending partner slot was not prepared.");
        }

        private static void UnresolvablePartnerSlotRefuses()
        {
            TestRunner.Equal(MidEncounterAdoption.Unavailable, Resolve(-1),
                "A partner outside the initiative order was adopted.");
            TestRunner.Equal(MidEncounterAdoption.Unavailable, Resolve(RiderSlot),
                "Rider and partner sharing one slot was adopted.");
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, true, false, true),
                "A surprised pending partner was prepared.");
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, false, true, true),
                "A surprise-round pending partner was prepared without proof it will act.");
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, false, false, false),
                "An invisible pending partner was prepared even though its slot is skipped.");
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, -1, RiderSlot + 1, false, false, true),
                "A rider outside the initiative order was adopted.");
        }

        private static void WrongTurnRefusesAdoption()
        {
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, false, RiderSlot, RiderSlot + 1, false, false, true),
                "Adoption accepted a turn that was not the exact rider's.");
        }

        private static void AdoptionRefusalReasons()
        {
            TestRunner.True(MidEncounterAdoptionPolicy.DescribeUnavailable(true, false, 1, 2)
                    .Contains("rider's current turn"),
                "The wrong-turn refusal does not name the rider's turn.");
            TestRunner.True(MidEncounterAdoptionPolicy.DescribeUnavailable(true, true, -1, 2)
                    .Contains("initiative order"),
                "The missing-roster refusal does not name the initiative order.");
            TestRunner.True(MidEncounterAdoptionPolicy.DescribeUnavailable(true, true, 2, 2)
                    .Contains("one initiative slot"),
                "The shared-slot refusal is not exact.");
            TestRunner.True(MidEncounterAdoptionPolicy.DescribeUnavailable(true, true, 1, 2)
                    .Contains("own turn in this round"),
                "The unresolvable-slot refusal is not exact.");
        }

        private static void AdoptionDoesNotPrepareThePrincipal()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            TestRunner.True(pair.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound),
                "Adoption of a running boundary was refused.");
            TestRunner.Equal(1L, pair.Sequence, "Adoption did not take exactly one activation sequence.");
            TestRunner.True(ReferenceEquals(pair.Boundary, turn), "Adoption bound a different boundary.");
            TestRunner.True(pair.State(rider).Granted && pair.State(rider).Prepared,
                "Adoption did not record the principal's already-completed native preparation.");
            // The principal's grant is recorded, so a second preparation attempt at
            // that boundary must be refused by the same rule an ordinary round uses.
            TestRunner.True(!pair.BeginActorPreparation(rider, turn),
                "Adoption left the principal open to a second preparation.");
            TestRunner.True(Threw(() => pair.FinishActorPreparation(rider)),
                "Adoption allowed the principal's preparation to be completed twice.");
        }

        // R1. A partner whose native initiative slot has already passed must not
        // become actionable again on the rider's adopted boundary. Its allocation is
        // recorded as granted, prepared AND ended.
        private static void AdoptionEndsSpentPartnerSlot()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            TestRunner.True(pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation),
                "Retaining adoption was refused.");
            var state = pair.State(mount);
            TestRunner.True(state.Granted && state.Prepared,
                "A spent partner slot was not recorded as having really happened.");
            TestRunner.True(state.Ended,
                "A spent partner slot was left open, so an allocation it already took could be used again.");
            TestRunner.True(!pair.CanAddress(mount, turn),
                "An already-acted partner could still be addressed inside the rider's adopted boundary.");
            TestRunner.True(pair.Open && pair.CanAddress(rider, turn),
                "Ending the spent partner also closed the rider's own running turn.");
            // Native timers due at this boundary still belong to the partner; only
            // permission to spend actions is closed.
            TestRunner.True(pair.OwnsRoundEffects(mount, turn),
                "An ended partner lost the round effects it still owns at this boundary.");
            TestRunner.True(!pair.BeginActorPreparation(mount, turn),
                "A spent partner was opened to a native preparation.");
            TestRunner.True(!pair.IsPreparingActor(mount, turn),
                "A spent partner was reported as preparing.");
            // Finalization must accept the already-ended partner without a second End.
            pair.BeginEnding();
            pair.EndActor(rider);
            TestRunner.True(pair.FinalizeActivation(), "An adopted pair with a spent partner could not finalize.");
        }

        // R1. Every shape of prior expenditure resolves the same way: the partner
        // keeps exactly the debt it stands at and stays unaddressable.
        private static void SpentPartnerVariantsKeepTheirDebt()
        {
            var cases = new[]
            {
                new { Name = "fully spent", Standard = 3f, Move = 3f, Swift = 3f },
                new { Name = "partially spent", Standard = 3f, Move = 0f, Swift = 0f },
                new { Name = "residual cooldown debt", Standard = 0.5f, Move = 1.25f, Swift = 0f },
                new { Name = "nothing spent", Standard = 0f, Move = 0f, Swift = 0f }
            };
            foreach (var expenditure in cases)
            {
                var rider = new object();
                var mount = new object();
                var turn = new object();
                var pair = new PairedActivation<object, object>(rider, mount);
                TestRunner.True(pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation),
                    "Retaining adoption was refused for the " + expenditure.Name + " partner.");
                var state = pair.State(mount);
                state.Observe(expenditure.Standard, expenditure.Move, expenditure.Swift);
                TestRunner.Equal(expenditure.Standard, state.StandardSpent,
                    "Observed Standard debt was lost for the " + expenditure.Name + " partner.");
                TestRunner.Equal(expenditure.Move, state.MoveSpent,
                    "Observed Move debt was lost for the " + expenditure.Name + " partner.");
                TestRunner.Equal(expenditure.Swift, state.SwiftSpent,
                    "Observed Swift debt was lost for the " + expenditure.Name + " partner.");
                TestRunner.True(state.Ended && !pair.CanAddress(mount, turn),
                    "The " + expenditure.Name + " partner became addressable again.");
                // Observation never lowers a recorded debt.
                state.Observe(0f, 0f, 0f);
                TestRunner.Equal(expenditure.Move, state.MoveSpent,
                    "A later observation reduced the " + expenditure.Name + " partner's recorded Move debt.");
            }
        }

        // R1. A skipped partner is never given participation. A later slot that
        // would have been skipped refuses the transition outright; an earlier slot
        // that was skipped is retained and ended, exactly like any spent slot.
        private static void SkippedPartnerSlotsNeverGainParticipation()
        {
            // Later slot, surprise-skipped.
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, true, false, true),
                "A surprised later partner slot was prepared.");
            // Later slot, acting in the surprise round.
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, false, true, true),
                "A later partner acting in the surprise round was prepared.");
            // Later slot, not visible to the player.
            TestRunner.Equal(MidEncounterAdoption.Unavailable,
                MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot + 1, false, false, false),
                "A later partner the native walk would skip for visibility was prepared.");
            // Earlier slot in each skipped shape: retained, and its allocation closed.
            var skipped = new[]
            {
                new { Name = "surprised", Surprised = true, Acting = false, Visible = true },
                new { Name = "acting in surprise round", Surprised = false, Acting = true, Visible = true },
                new { Name = "not visible", Surprised = false, Acting = false, Visible = false }
            };
            foreach (var shape in skipped)
            {
                TestRunner.Equal(MidEncounterAdoption.RetainPartnerParticipation,
                    MidEncounterAdoptionPolicy.Resolve(true, true, RiderSlot, RiderSlot - 1,
                        shape.Surprised, shape.Acting, shape.Visible),
                    "An earlier " + shape.Name + " partner changed the disposition.");
                var rider = new object();
                var mount = new object();
                var turn = new object();
                var pair = new PairedActivation<object, object>(rider, mount);
                pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation);
                TestRunner.True(pair.State(mount).Ended && !pair.CanAddress(mount, turn),
                    "An earlier " + shape.Name + " partner became addressable on the rider's boundary.");
            }
        }

        // R1. Eligibility returns only through Kingmaker's next lawful allocation.
        private static void SpentPartnerRecoversOnlyNextAllocation()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation);
            pair.BeginEnding();
            pair.EndActor(rider);
            TestRunner.True(pair.FinalizeActivation(), "The adopted allocation could not finalize.");
            var nextTurn = new object();
            TestRunner.True(pair.Begin(nextTurn), "The next lawful allocation was refused.");
            TestRunner.True(!pair.State(mount).Ended && !pair.State(mount).Granted,
                "The next allocation carried the previous round's closed partner state forward.");
            TestRunner.True(pair.BeginActorPreparation(mount, nextTurn),
                "The partner could not be prepared by its next lawful allocation.");
        }

        private static void AdoptionReservesPartnerPreparation()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            pair.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound);
            TestRunner.True(!pair.State(mount).Granted,
                "A pending partner was granted before its native preparation.");
            TestRunner.True(!pair.Open, "The pair was open before the partner finished preparing.");
            TestRunner.True(pair.BeginActorPreparation(mount, turn),
                "The partner's one native preparation could not be reserved.");
            TestRunner.True(pair.IsPreparingActor(mount, turn), "The reserved partner grant is not observable.");
            pair.FinishActorPreparation(mount);
            TestRunner.True(pair.Open && pair.CanAddress(mount, turn),
                "The prepared partner could not be addressed inside the adopted boundary.");
            TestRunner.True(!pair.BeginActorPreparation(mount, turn),
                "The partner was opened to a second native preparation.");
        }

        private static void AdoptionRefusesUnusableBoundary()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();

            var used = new PairedActivation<object, object>(rider, mount);
            used.Begin(turn);
            TestRunner.True(!used.AdoptRunningBoundary(new object(), MidEncounterAdoption.PreparePartnerThisRound),
                "An activation that already began was adopted again.");

            var twice = new PairedActivation<object, object>(rider, mount);
            TestRunner.True(twice.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound),
                "First adoption refused.");
            TestRunner.True(!twice.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound),
                "The same boundary was adopted twice.");

            var split = new PairedActivation<object, object>(rider, mount);
            split.Detach();
            TestRunner.True(!split.AdoptRunningBoundary(turn, MidEncounterAdoption.PreparePartnerThisRound),
                "A detached activation adopted a running boundary.");

            var noBoundary = new PairedActivation<object, object>(rider, mount);
            TestRunner.True(!noBoundary.AdoptRunningBoundary(null, MidEncounterAdoption.PreparePartnerThisRound),
                "Adoption accepted an absent native boundary.");
            TestRunner.True(!noBoundary.AdoptRunningBoundary(turn, MidEncounterAdoption.Unavailable),
                "Adoption accepted an unavailable disposition.");
            TestRunner.True(!noBoundary.AdoptRunningBoundary(turn, MidEncounterAdoption.RealTimeOwnership),
                "Adoption bound a boundary under the real-time disposition.");
            TestRunner.Equal(0L, noBoundary.Sequence, "A refused adoption consumed an activation sequence.");
        }

        private static void AdoptedActivationFinalizes()
        {
            var rider = new object();
            var mount = new object();
            var adoptedTurn = new object();
            var nextTurn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            pair.AdoptRunningBoundary(adoptedTurn, MidEncounterAdoption.RetainPartnerParticipation);
            // An adopted boundary is a real activation: the existing rule that a
            // new one cannot begin before finalization applies to it unchanged.
            TestRunner.True(Threw(() => pair.Begin(nextTurn)),
                "The next round began while the adopted activation was unfinalized.");
            TestRunner.True(Threw(() => pair.FinalizeActivation()),
                "The adopted activation finalized while its granted actors were unended.");
            pair.EndActor(rider);
            pair.EndActor(mount);
            TestRunner.True(pair.FinalizeActivation(), "The adopted activation could not be finalized.");
            TestRunner.True(pair.Begin(nextTurn), "The next native round could not begin normally after adoption.");
            TestRunner.Equal(2L, pair.Sequence, "The round after adoption did not take exactly one more sequence.");
            TestRunner.True(!pair.Rider.Prepared && !pair.Mount.Prepared,
                "The round after adoption inherited adoption's bookkeeping preparation.");
        }

        private static void AdoptionPreservesDebt()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation);
            pair.State(rider).Observe(6f, 3f, 0f);
            pair.State(mount).Observe(6f, 6f, 6f);
            // A later observation can only ever raise an observed debt.
            pair.State(rider).Observe(0f, 0f, 0f);
            pair.State(mount).Observe(0f, 0f, 0f);
            TestRunner.Equal(6f, pair.State(rider).StandardSpent, "Adopted rider Standard debt fell.");
            TestRunner.Equal(3f, pair.State(rider).MoveSpent, "Adopted rider Move debt fell.");
            TestRunner.Equal(6f, pair.State(mount).MoveSpent, "Adopted mount Move debt fell.");
            TestRunner.Equal(6f, pair.State(mount).SwiftSpent, "Adopted mount Swift debt fell.");

            var snapshot = pair.Capture();
            TestRunner.Equal(1L, snapshot.Sequence, "Adoption did not persist its exact sequence.");
            TestRunner.True(snapshot.Rider.Granted && snapshot.Rider.Prepared,
                "Adoption's principal grant did not persist.");
            TestRunner.Equal(3f, snapshot.Rider.MoveObserved, "Adopted rider Move debt did not persist.");
            TestRunner.Equal(6f, snapshot.Mount.SwiftObserved, "Adopted mount Swift debt did not persist.");
        }

        private static void LedgerSuppressesDuplicateControl()
        {
            var ledger = new MountedTransitionLedger();
            MountedTransitionRecord first;
            string refusal;
            TestRunner.True(ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount,
                    "shell:1", "rider", "mount", 4L, out first, out refusal),
                "The first voluntary Mount was refused: " + refusal);
            ledger.Settle(first, true);
            TestRunner.Equal(1L, ledger.AcceptedMountCount, "One accepted Mount was not counted once.");

            MountedTransitionRecord repeat;
            TestRunner.True(!ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount,
                    "shell:1", "rider", "mount", 5L, out repeat, out refusal),
                "A repeated delivery of the same native control created a second transition.");
            TestRunner.Equal(null, repeat, "A suppressed duplicate returned a record.");
            TestRunner.True(refusal.Contains("already been delivered"), "The duplicate refusal is not exact.");
            TestRunner.Equal(1L, ledger.AcceptedMountCount, "A duplicate delivery counted a second Mount.");
            TestRunner.Equal(1L, ledger.AdmittedMountCount, "A duplicate delivery was admitted.");
            TestRunner.Equal(1L, ledger.DuplicateControlSuppressedCount, "The duplicate was not recorded.");

            var found = ledger.Find("shell:1");
            TestRunner.True(found != null && found.Settled && found.Accepted && found.GenerationBefore == 4L,
                "The first control's own record was lost or rewritten.");
        }

        private static void LedgerRefusesConcurrentTransition()
        {
            var ledger = new MountedTransitionLedger();
            MountedTransitionRecord first;
            MountedTransitionRecord second;
            string refusal;
            ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, "shell:1", "rider", "mount", 0L,
                out first, out refusal);
            TestRunner.True(ledger.HasVoluntaryTransitionInFlight, "An admitted transition is not in flight.");
            TestRunner.Equal("shell:1", ledger.InFlightControlIdentity, "The in-flight control is not exact.");
            TestRunner.True(!ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryDismount, "shell:2",
                    "rider", "mount", 0L, out second, out refusal),
                "A second voluntary transition was admitted while one was in flight.");
            TestRunner.True(refusal.Contains("already in flight"), "The concurrent refusal is not exact.");
            TestRunner.Equal(1L, ledger.ConcurrentControlSuppressedCount, "The concurrent attempt was not recorded.");
            TestRunner.True(Threw(() => ledger.Settle(second, true)),
                "A refused transition could be settled.");

            ledger.Settle(first, false);
            TestRunner.True(!ledger.HasVoluntaryTransitionInFlight, "A settled transition stayed in flight.");
            TestRunner.Equal(0L, ledger.AcceptedMountCount, "A refused transition counted as accepted.");
            TestRunner.Equal(1L, ledger.RefusedVoluntaryCount, "A refused transition was not counted.");
            TestRunner.True(ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryDismount, "shell:2",
                    "rider", "mount", 0L, out second, out refusal),
                "A new control was refused after the previous transition settled: " + refusal);
            ledger.Settle(second, true);
            TestRunner.Equal(1L, ledger.AcceptedDismountCount, "The Dismount was not counted once.");
            TestRunner.True(Threw(() => ledger.Settle(second, true)), "A settled record was settled twice.");
        }

        private static void LedgerSeparatesForcedDetach()
        {
            var ledger = new MountedTransitionLedger();
            TestRunner.True(ledger.RecordForcedDetach("rider", "mount", 7L, "Death"),
                "A forced detach was not recorded.");
            TestRunner.Equal(1L, ledger.ForcedDetachCount, "Forced detach was not counted.");
            TestRunner.Equal(0L, ledger.AcceptedDismountCount, "Forced detach booked a voluntary Dismount.");
            TestRunner.Equal(0L, ledger.AdmittedDismountCount, "Forced detach was admitted as voluntary.");
            TestRunner.True(!ledger.HasVoluntaryTransitionInFlight, "Forced detach opened a voluntary transition.");

            // Cleanup is never gated by an in-flight voluntary transition.
            MountedTransitionRecord voluntary;
            string refusal;
            ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, "shell:1", "rider", "mount", 8L,
                out voluntary, out refusal);
            TestRunner.True(ledger.RecordForcedDetach("rider", "mount", 8L, "Exception"),
                "Cleanup was blocked by an in-flight voluntary transition.");
            ledger.Settle(voluntary, false);
            TestRunner.Equal(2L, ledger.ForcedDetachCount, "The second cleanup was not counted.");
            TestRunner.Equal(0L, ledger.AcceptedMountCount, "Cleanup accepted the refused Mount.");
        }

        private static void LedgerForcedDetachIsIdempotent()
        {
            var ledger = new MountedTransitionLedger();
            TestRunner.True(ledger.RecordForcedDetach("rider", "mount", 3L, "AreaTransition"),
                "The first cleanup was not recorded.");
            TestRunner.True(!ledger.RecordForcedDetach("rider", "mount", 3L, "AreaTransition"),
                "Repeated cleanup for one generation was recorded twice.");
            TestRunner.Equal(1L, ledger.ForcedDetachCount, "Repeated cleanup incremented the forced count.");
            TestRunner.True(ledger.RecordForcedDetach("rider", "mount", 4L, "ModDisabled"),
                "Cleanup of a later generation was suppressed as a duplicate.");
            TestRunner.Equal(2L, ledger.ForcedDetachCount, "A distinct generation's cleanup was not counted.");
        }

        private static void LedgerRequiresControlIdentity()
        {
            var ledger = new MountedTransitionLedger();
            MountedTransitionRecord record;
            string refusal;
            TestRunner.True(!ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, null,
                    "rider", "mount", 0L, out record, out refusal),
                "A voluntary transition was admitted without a control identity.");
            TestRunner.True(refusal.Contains("exact native control identity"), "The identity refusal is not exact.");
            TestRunner.True(!ledger.TryAdmitVoluntary(MountedTransitionKind.VoluntaryMount, "   ",
                    "rider", "mount", 0L, out record, out refusal),
                "A blank control identity was admitted.");
            TestRunner.True(!ledger.HasVoluntaryTransitionInFlight,
                "A refused admission left a transition in flight.");
            TestRunner.Equal(0L, ledger.AdmittedMountCount, "A refused admission counted as admitted.");
        }

        private static void LedgerRefusesForcedAsVoluntary()
        {
            var ledger = new MountedTransitionLedger();
            MountedTransitionRecord record;
            string refusal;
            TestRunner.True(Threw(() => ledger.TryAdmitVoluntary(MountedTransitionKind.ForcedDetach, "shell:1",
                    "rider", "mount", 0L, out record, out refusal)),
                "Forced detach was admitted through the voluntary path.");
        }

        private static bool Threw(Action action)
        {
            try
            {
                action();
                return false;
            }
            catch (Exception)
            {
                return true;
            }
        }
    }
}
