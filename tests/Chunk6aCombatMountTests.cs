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
            runner.Run("adoption retains a spent partner without preparing it", AdoptionRetainsSpentPartner);
            runner.Run("adoption reserves one partner preparation when its slot is pending", AdoptionReservesPartnerPreparation);
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

        private static void AdoptionRetainsSpentPartner()
        {
            var rider = new object();
            var mount = new object();
            var turn = new object();
            var pair = new PairedActivation<object, object>(rider, mount);
            TestRunner.True(pair.AdoptRunningBoundary(turn, MidEncounterAdoption.RetainPartnerParticipation),
                "Retaining adoption was refused.");
            TestRunner.True(pair.State(mount).Granted && pair.State(mount).Prepared,
                "A retained partner was not recorded as already prepared.");
            TestRunner.True(!pair.State(mount).Ended,
                "A retained partner was ended, forfeiting native capacity it still has.");
            TestRunner.True(pair.Open, "A retained pair could not address its own actors.");
            TestRunner.True(pair.CanAddress(mount, turn),
                "A retained partner could not be addressed inside the adopted boundary.");
            TestRunner.True(!pair.BeginActorPreparation(mount, turn),
                "A retained partner was opened to a native preparation.");
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
