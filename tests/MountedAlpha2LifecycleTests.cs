using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Alpha 2 review round 4 (R3-F1, R3-F2, R3-F3, R1-F5, R1-F8, R1-F11, R2-F1, R2-F5). Each check drives the production
    // decision object the controller itself uses (MountedRetainedOrderLedger, MountedHoldProvenance, MountedPreStartApproach-
    // Policy, PairedAttackClickAdmissionPolicy, MountedEngagementRecord, UnifiedMountedStockAttackPolicy, MountedAutomatic-
    // AttackPolicy) with the facts the controller derives at its native seams. The native seams themselves (acceptance
    // postfixes, the pre-start TickApproaching hook, the click probe) are pinned by scripts/Test-MountedAlpha2NativeContract.ps1;
    // none of this is a game run.
    internal static class MountedAlpha2LifecycleTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("R3-F3: the order's own delegated move and its repath replacement do not suppress horse follow-through", OwnMovesKeepFollowThrough);
            runner.Run("R1-F5: newer work accepted by slot or queue overrides the order whether queued or finished; refused work never does", AcceptedWorkOverrides);
            runner.Run("R2-F1: the attack kind fixed at acceptance survives a deferred first drive and a weapon change", AttackKindAtAcceptance);
            runner.Run("R2-F5: newer player work in a mount slot retires a waiting real-time order; own carrier moves do not", RealTimeMountSlotSuccessor);
            runner.Run("R4-F3: an explicit rider order survives older mount-slot work and yields to newer work", ExplicitOrderVersusOlderSlotWork);
            runner.Run("R5-F5: promotion of older queued work is not a new override; insertion after the order is", PromotionIsNotAcceptance);
            runner.Run("R4-F8: a sustained order releases finished command identities and keeps the live carrier", LedgerReleasesFinished);
            runner.Run("PR-1 R1-F1: turn-based follow-through is offered only while the horse's strike is in its current reach; explicit mount orders keep their approach", FollowThroughNeedsReach);
            runner.Run("PR-1 R1-F3: an actor-specific rider end is remembered for its generation only; the rider is not redispatched", RiderEndedLedger);
            runner.Run("PR-1 R1-F10: native's last-target +1 does not outrank a nearer in-reach enemy on fresh acquisition; manual target, live order and ranged controls", FreshAcquisitionTieBreak);
            runner.Run("R1-F8: any mount Hold restricts automatic movement except an own-carrier write with positive provenance", MountHoldProvenance);
            runner.Run("R3-F1: rider or horse striker moves before readiness, queued orders move after promotion; native keeps the start", PreStartApproach);
            runner.Run("R1-F11: engagement attribution credits only the same exact carrier across an interval", EngagementAttribution);
            runner.Run("R1-F11: equally ranked eligible native candidates resolve to the nearest without oscillation", EqualRankTieBreak);
            runner.Run("R1-F11: model of the traced native target comparison (two eligible, nearer ineligible, manual target)", NativeTargetChoice);
        }

        // Review R5-F5: a stationary player Standard queued BEFORE the explicit rider click, then promoted, is older work; the
        // same Standard inserted AFTER the click supersedes the order at its insertion and stays superseding when promoted.
        private static void PromotionIsNotAcceptance()
        {
            var standard = new MountedAcceptedWorkFacts { PlayerOrder = true, NeedsApproach = false };
            var before = new MountedRetainedOrderLedger();
            var queuedEarlier = new object();          // queued on the horse before the click: never seen by this order
            before.Begin(51, false, true);             // the explicit rider click is accepted
            TestRunner.Equal(MountedAcceptedWorkClass.Competing, before.ObserveAccepted(queuedEarlier, standard, MountedAcceptedWorkRoute.Promotion, "mount:promoted"),
                "The promoted command was misclassified.");
            TestRunner.True(!before.Superseded(51) && before.FollowThroughAllowed(51), "Promotion of older queued work superseded the newer click.");
            var after = new MountedRetainedOrderLedger();
            after.Begin(52, false, true);
            var insertedLater = new object();
            after.ObserveAccepted(insertedLater, standard, MountedAcceptedWorkRoute.Queue, "mount:queued");
            TestRunner.True(after.Superseded(52), "Direct queue insertion after the click did not supersede it.");
            after.ObserveAccepted(insertedLater, standard, MountedAcceptedWorkRoute.Promotion, "mount:promoted");
            TestRunner.True(after.Superseded(52), "Promotion cleared a real override.");
        }

        private static readonly MountedAcceptedWorkFacts PlayerMove = new MountedAcceptedWorkFacts { PlayerOrder = true, NeedsApproach = true };

        private static MountedStockAttackDecision FollowThrough(MountedRetainedOrderLedger order, long generation, bool riderReady, bool horseReady) =>
            UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, riderReady, horseReady, false, true,
                riderIsLegalActor: true,
                mountIsLegalActor: UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, false, false, true, false, true,
                    order.IsCurrent(generation) && order.Melee, order.Superseded(generation)));

        private static void OwnMovesKeepFollowThrough()
        {
            var order = new MountedRetainedOrderLedger();
            order.Begin(7, false, true);
            var wrapper = new object(); var firstMove = new object(); var repathMove = new object();
            // The controller records the wrapper before native Run; the command announces each exact move before submitting it.
            order.RecordOwnIssued(7, wrapper);
            order.RecordOwnIssued(7, firstMove);
            // Native accepts the delegated move into the mount's Move slot: it is CreatedByPlayer and needs approach, yet it is
            // the order's own movement.
            TestRunner.Equal(MountedAcceptedWorkClass.RetainedOwn, order.ObserveAccepted(firstMove, PlayerMove, MountedAcceptedWorkRoute.Slot, "move"),
                "The order's own delegated move was classified as newer work.");
            order.RecordOwnIssued(7, repathMove);
            TestRunner.Equal(MountedAcceptedWorkClass.RetainedOwn, order.ObserveAccepted(repathMove, PlayerMove, MountedAcceptedWorkRoute.Slot, "repath"),
                "The repath replacement was classified as newer work.");
            TestRunner.True(order.FollowThroughAllowed(7) && order.OwnCommandCount == 3, "Own movement suppressed follow-through.");
            // Rider approached and struck (rider spent): exactly one eligible horse dispatch follows.
            TestRunner.Equal(MountedStockAttackDecision.DispatchRider, FollowThrough(order, 7, true, true), "The rider did not strike first.");
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, FollowThrough(order, 7, false, true), "The horse did not follow the approached rider.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, FollowThrough(order, 7, false, false), "A spent horse attacked again.");
            // Negative control: the same move shape, but a genuine player order that this order never issued.
            var playerMove = new object();
            TestRunner.Equal(MountedAcceptedWorkClass.Competing, order.ObserveAccepted(playerMove, PlayerMove, MountedAcceptedWorkRoute.Slot, "player"),
                "A genuine player move was treated as the order's own.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, FollowThrough(order, 7, false, true), "The horse attacked after a newer player order.");
            // A move announced for an older generation is never adopted by a newer order.
            var newer = new MountedRetainedOrderLedger(); newer.Begin(8, false, true);
            newer.RecordOwnIssued(7, firstMove);
            TestRunner.True(!newer.IsRetainedOwn(firstMove), "An older order's move was adopted by a newer order.");
        }

        private static void AcceptedWorkOverrides()
        {
            MountedRetainedOrderLedger Fresh() { var o = new MountedRetainedOrderLedger(); o.Begin(3, false, true); return o; }
            // Direct queue insertion (AddToQueue): a queued Standard cast on the horse overrides immediately.
            var queued = Fresh();
            var cast = new MountedAcceptedWorkFacts { PlayerOrder = true, NeedsApproach = false };
            TestRunner.Equal(MountedAcceptedWorkClass.Competing, queued.ObserveAccepted(new object(), cast, MountedAcceptedWorkRoute.Queue, "cast"),
                "Directly queued work was not recorded.");
            TestRunner.True(queued.Superseded(3) && queued.OverrideDetail.StartsWith("Queue:", StringComparison.Ordinal), "The queue route was lost.");
            // Work that completes before the rider: the latch persists after the command has left the slot.
            var completed = Fresh();
            completed.ObserveAccepted(new object(), cast, MountedAcceptedWorkRoute.Slot, "finished");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, FollowThrough(completed, 3, false, true), "Finished newer work was forgotten.");
            // A refused submission never reaches an acceptance seam: nothing is recorded.
            var refused = Fresh();
            TestRunner.True(refused.FollowThroughAllowed(3), "A refused submission was recorded.");
            // Coexisting Free/Swift (in reach or started) does not override; one that must move its actor does.
            var free = Fresh();
            TestRunner.Equal(MountedAcceptedWorkClass.CoexistingFreeOrSwift,
                free.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { PlayerOrder = true, FreeOrSwift = true }, MountedAcceptedWorkRoute.Slot, "swift"),
                "A coexisting Swift action overrode the order.");
            TestRunner.True(free.FollowThroughAllowed(3), "A coexisting Free/Swift action suppressed follow-through.");
            TestRunner.Equal(MountedAcceptedWorkClass.Competing,
                free.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { PlayerOrder = true, FreeOrSwift = true, NeedsApproach = true }, MountedAcceptedWorkRoute.Slot, "swift-approach"),
                "A Free/Swift action that must move its actor was ignored.");
            // Reactions, the pair's own commands and brain work never override.
            var own = Fresh();
            TestRunner.Equal(MountedAcceptedWorkClass.Reaction, own.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { Reaction = true, PlayerOrder = true }, MountedAcceptedWorkRoute.Slot, "aoo"), "A reaction overrode.");
            TestRunner.Equal(MountedAcceptedWorkClass.PairCommand, own.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { PairCommand = true, PlayerOrder = true }, MountedAcceptedWorkRoute.Queue, "pair"), "A pair command overrode.");
            TestRunner.Equal(MountedAcceptedWorkClass.NotPlayerWork, own.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { AiOrigin = true, PlayerOrder = true }, MountedAcceptedWorkRoute.Slot, "ai"), "Brain work overrode.");
            TestRunner.True(own.FollowThroughAllowed(3) && own.CompetingCount == 0, "Non-competing work latched.");
            // The same command observed by both seams (Run's queue-instead path reaches AddToQueueInternal inside Run) latches once.
            var twice = Fresh(); var same = new object();
            twice.ObserveAccepted(same, cast, MountedAcceptedWorkRoute.Queue, "a"); twice.ObserveAccepted(same, cast, MountedAcceptedWorkRoute.Queue, "a");
            TestRunner.True(twice.Superseded(3), "Double observation lost the latch.");
            // A cleared order (the intent ended) records nothing.
            var cleared = Fresh(); cleared.Clear();
            cleared.ObserveAccepted(new object(), cast, MountedAcceptedWorkRoute.Slot, "late");
            TestRunner.True(!cleared.Superseded(3) && !cleared.FollowThroughAllowed(3), "A cleared order recorded work or allowed follow-through.");
        }

        private static void AttackKindAtAcceptance()
        {
            // A bow order accepted at Begin; its first eligible drive is deferred (the turn is not yet acting) and the rider
            // switches to melee before it: the kind stays the one accepted.
            var bow = new MountedRetainedOrderLedger();
            bow.Begin(11, false, false);
            var riderWeaponNowMelee = true;
            var meleeOrder = bow.IsCurrent(11) ? bow.Melee : riderWeaponNowMelee;
            TestRunner.True(!meleeOrder, "A deferred drive re-derived the kind from the current weapon.");
            TestRunner.Equal(MountedStockAttackDecision.CompleteTurnBasedIntent, FollowThrough(bow, 11, false, true), "A retained bow order spent the horse's Standard.");
            var melee = new MountedRetainedOrderLedger();
            melee.Begin(12, false, true);
            TestRunner.Equal(MountedStockAttackDecision.DispatchMount, FollowThrough(melee, 12, false, true), "The accepted melee order did not reach the horse.");
            // Promotion of an automatic order by the player's click keeps its kind.
            var auto = new MountedRetainedOrderLedger(); auto.Begin(13, true, true); auto.PromoteToPlayer();
            TestRunner.True(!auto.Automatic && auto.Melee, "Promotion changed the accepted kind or origin.");
        }

        private static void RealTimeMountSlotSuccessor()
        {
            var order = new MountedRetainedOrderLedger();
            order.Begin(21, true, true);
            var preStartMove = new object();
            order.RecordOwnIssued(21, preStartMove);
            order.ObserveAccepted(preStartMove, PlayerMove, MountedAcceptedWorkRoute.Slot, "own");
            // The wrapper waits for readiness; the order's own carrier occupies the mount slot: no retirement.
            var ownSlot = new List<MountedAutomaticSlotFacts> { new MountedAutomaticSlotFacts { PairOwnedOrReaction = true } };
            var newer = MountedRetainedOrderLedger.NewerPlayerWork(true, MountedAutomaticAttackPolicy.HasPlayerOperation(ownSlot, 0), order.Superseded(21));
            TestRunner.True(MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false, false, true, newer) == null,
                "The order's own carrier retired its order.");
            // A newer player command put directly into a mount slot while the old attack waits retires it on the update an
            // actor becomes strike-ready, before any dispatch.
            order.ObserveAccepted(new object(), PlayerMove, MountedAcceptedWorkRoute.Slot, "mount:UnitMoveTo");
            var playerSlot = new List<MountedAutomaticSlotFacts> { new MountedAutomaticSlotFacts() };
            newer = MountedRetainedOrderLedger.NewerPlayerWork(true, MountedAutomaticAttackPolicy.HasPlayerOperation(playerSlot, 0), order.Superseded(21));
            TestRunner.Equal("newer player operation", MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false, false, true, newer),
                "A newer mount-slot order did not retire the waiting order.");
        }

        // Review R4-F3: both orderings of a stationary player Standard on the horse and an explicit rider click.
        private static void ExplicitOrderVersusOlderSlotWork()
        {
            var occupied = new List<MountedAutomaticSlotFacts> { new MountedAutomaticSlotFacts() };
            var live = MountedAutomaticAttackPolicy.HasPlayerOperation(occupied, 0);
            // 1. The horse's Standard was already running when the rider was clicked: the newer click survives it.
            var click = new MountedRetainedOrderLedger(); click.Begin(31, false, true);
            TestRunner.True(MountedAutomaticAttackPolicy.RetireReason(true, false, true, true, true, false, false, true,
                MountedRetainedOrderLedger.NewerPlayerWork(false, live, click.Superseded(31))) == null, "An older mount action retired a newer rider click.");
            // 2. A newer mount command accepted after the click still retires the older rider order.
            click.ObserveAccepted(new object(), new MountedAcceptedWorkFacts { PlayerOrder = true }, MountedAcceptedWorkRoute.Slot, "mount:cast");
            TestRunner.Equal("newer player operation", MountedAutomaticAttackPolicy.RetireReason(true, false, true, true, true, false, false, true,
                MountedRetainedOrderLedger.NewerPlayerWork(false, live, click.Superseded(31))), "A newer mount command did not retire the older rider order.");
            // Automatic proposals keep the occupied-slot protection.
            var auto = new MountedRetainedOrderLedger(); auto.Begin(32, true, true);
            TestRunner.True(MountedRetainedOrderLedger.NewerPlayerWork(true, live, auto.Superseded(32)), "An automatic order ignored an occupied slot.");
        }

        // PR-1 review R1-F1: a reach-weapon rider has struck; the horse has its Standard but stands outside its natural reach and
        // cannot move. The production selection (MountJoinsIntent feeding DecideNext) offers no horse dispatch; in reach it does.
        private static void FollowThroughNeedsReach()
        {
            Func<bool, bool> joins = inReach => UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, false, false, true, false, true, true, false, inReach);
            TestRunner.True(!joins(false) && joins(true), "Follow-through ignored the horse's reach.");
            TestRunner.True(UnifiedMountedStockAttackPolicy.MountJoinsIntent(true, true, false, true, false, true, true, false, false),
                "An explicit mount order lost its own approach.");
            var outside = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, false, true, false, false, true, joins(false), false);
            var inside = UnifiedMountedStockAttackPolicy.DecideNext(true, true, false, true, false, true, false, true, true, joins(true), false);
            TestRunner.True(outside != MountedStockAttackDecision.DispatchMount && inside == MountedStockAttackDecision.DispatchMount,
                "Horse dispatch outside reach: " + outside + "; in reach: " + inside);
        }

        // PR-1 review R1-F3: the rider-ended mark belongs to one generation.
        private static void RiderEndedLedger()
        {
            var ledger = new MountedRetainedOrderLedger();
            ledger.Begin(4, false, true);
            TestRunner.True(!ledger.RiderEnded(4), "A fresh order started with its rider ended.");
            ledger.MarkRiderEnded(3);
            TestRunner.True(!ledger.RiderEnded(4) && !ledger.RiderEnded(3), "A stale generation marked the order.");
            ledger.MarkRiderEnded(4);
            TestRunner.True(ledger.RiderEnded(4) && ledger.FollowThroughAllowed(4), "The rider end was not kept, or it ended the horse's follow-through.");
            ledger.Begin(5, false, true);
            TestRunner.True(!ledger.RiderEnded(5), "A new order inherited the rider end.");
        }

        // PR-1 review R1-F10: the production selection seam (NativeAttackTieBreak.Choose) with native's weights.
        private static void FreshAcquisitionTieBreak()
        {
            object farther = "farther-last-target", nearer = "nearer";
            var ranked = new List<NativeRankedCandidate>
            {
                new NativeRankedCandidate { Unit = farther, Weight = 1f, Distance = 3f, Eligible = true },
                new NativeRankedCandidate { Unit = nearer, Weight = 0f, Distance = 1.5f, Eligible = true }
            };
            TestRunner.True(ReferenceEquals(NativeAttackTieBreak.Choose(ranked, farther, null, true), nearer), "Fresh acquisition kept the farther last target.");
            TestRunner.True(ReferenceEquals(NativeAttackTieBreak.Choose(ranked, farther, farther, true), farther), "A live order lost its target.");
            TestRunner.True(ReferenceEquals(NativeAttackTieBreak.Choose(ranked, farther, null, false), farther), "A ranged proposal was retargeted.");
            var manual = new List<NativeRankedCandidate>
            {
                new NativeRankedCandidate { Unit = farther, Weight = 1000001f, Distance = 3f, Eligible = true },
                new NativeRankedCandidate { Unit = nearer, Weight = 0f, Distance = 1.5f, Eligible = true }
            };
            TestRunner.True(ReferenceEquals(NativeAttackTieBreak.Choose(manual, farther, null, true), farther), "A manual target was outranked.");
            var ineligible = new List<NativeRankedCandidate>
            {
                new NativeRankedCandidate { Unit = farther, Weight = 1f, Distance = 3f, Eligible = true },
                new NativeRankedCandidate { Unit = nearer, Weight = 0f, Distance = 1.5f, Eligible = false }
            };
            TestRunner.True(ReferenceEquals(NativeAttackTieBreak.Choose(ineligible, farther, null, true), farther), "An ineligible nearer enemy was chosen.");
        }

        // Review R4-F8: a sustained order releases finished identities and keeps the live repath replacement.
        private static void LedgerReleasesFinished()
        {
            var order = new MountedRetainedOrderLedger(); order.Begin(41, true, true);
            var finished = new HashSet<object>();
            var wrapper = new object(); order.RecordOwnIssued(41, wrapper);
            object live = null;
            for (var strike = 0; strike < 20; strike++)
            {
                var move = new object(); order.RecordOwnIssued(41, move);
                if (live != null) finished.Add(live);
                live = move;
                order.ReleaseFinished(finished.Contains);
            }
            TestRunner.True(order.OwnCommandCount == 2 && order.IsRetainedOwn(live) && order.IsRetainedOwn(wrapper),
                "Finished identities were retained (" + order.OwnCommandCount + ") or the live carrier was forgotten.");
        }

        private static void MountHoldProvenance()
        {
            var hold = new MountedHoldProvenance();
            // Hold set on the horse before the pair was established (or left in a save), rider unheld: honored.
            TestRunner.True(MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHoldRestrictsAutomatic(true)),
                "A Hold that predates the pair let the horse travel automatically.");
            // A Hold change during travel: the next update's retirement stops the automatic approach.
            TestRunner.Equal("hold out of reach", MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false,
                MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHoldRestrictsAutomatic(true)), false, false),
                "A Hold during travel did not stop the automatic approach.");
            // Unheld horse: lawful.
            TestRunner.True(!MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHoldRestrictsAutomatic(false)),
                "An unheld horse was restricted.");
            // Exact carrier-produced Hold: the OnAction write is undone; only an undo that could not apply leaves positive
            // provenance, and that one Hold alone is not honored.
            TestRunner.True(MountedHoldProvenance.RestoreHoldAfterMoveAction(true, false, true) &&
                !MountedHoldProvenance.RestoreHoldAfterMoveAction(false, false, true) &&
                !MountedHoldProvenance.RestoreHoldAfterMoveAction(true, true, true), "The carrier Hold restore was misapplied.");
            hold.ObserveUnrestoredCarrierHold();
            TestRunner.True(!hold.MountHoldRestrictsAutomatic(true), "A carrier-produced Hold with positive provenance restricted the pair.");
            // The player's Hold or an explicit Stop/release ends that provenance.
            hold.ObservePlayerHoldInput(true);
            TestRunner.True(hold.MountHoldRestrictsAutomatic(true), "The player's Hold after a carrier write was not honored.");
            hold.ObserveUnrestoredCarrierHold(); hold.Observe(false);
            TestRunner.True(!hold.CarrierInducedHold && hold.MountHoldRestrictsAutomatic(true), "A released Hold kept its old provenance.");
            TestRunner.True(MountedHoldProvenance.AutomaticMovementForbidden(true, false, true, false) &&
                MountedHoldProvenance.AutomaticMovementForbidden(false, true, true, false) &&
                MountedHoldProvenance.AutomaticMovementForbidden(false, false, false, false), "Rider Hold, a completed player move or an immobile mount lost its effect.");
            TestRunner.True(MountedHoldProvenance.NativeRunKeepsMountHold(true, true) && !MountedHoldProvenance.NativeRunKeepsMountHold(true, false) &&
                !MountedHoldProvenance.NativeRunKeepsMountHold(false, true), "Run's Hold keep was misapplied.");
        }

        private static void PreStartApproach()
        {
            // Striker selection: the rider when it may strike, otherwise the horse; an explicit horse order is the horse's.
            TestRunner.Equal(MountedEarlyStriker.Rider, MountedPreStartApproachPolicy.SelectStriker(false, true, true), "The rider was not the striker.");
            // Review R5-F6: a temporarily unable rider (dazed) never converts its order into a horse attack: no early striker.
            TestRunner.Equal(MountedEarlyStriker.None, MountedPreStartApproachPolicy.SelectStriker(false, false, true), "A rider order became a horse attack.");
            TestRunner.Equal(MountedEarlyStriker.Mount, MountedPreStartApproachPolicy.SelectStriker(true, true, true), "An explicit horse order went to the rider.");
            TestRunner.Equal(MountedEarlyStriker.None, MountedPreStartApproachPolicy.SelectStriker(true, true, false), "A restricted horse struck.");
            TestRunner.Equal(MountedEarlyStriker.None, MountedPreStartApproachPolicy.SelectStriker(false, false, false), "A striker was invented.");
            // Dispatch before readiness: planned strike not admissible where the pair stands, no actor ready, movement lawful.
            bool Early(bool selected = true, bool forbidden = false, bool admissible = false, bool riderReady = false, bool mountReady = false, bool realTime = true) =>
                MountedPreStartApproachPolicy.DispatchBeforeStrikeReadiness(realTime, selected, forbidden, admissible, riderReady, mountReady);
            TestRunner.True(Early(), "An out-of-reach order waited for strike readiness before moving.");
            TestRunner.True(!Early(admissible: true) && !Early(riderReady: true) && !Early(mountReady: true) && !Early(selected: false) &&
                !Early(forbidden: true) && !Early(realTime: false), "Dispatch before readiness applied where it must not.");
            MountedPreStartApproachAction Step(bool inSlot = true, bool started = false, bool perceived = true, bool forbidden = false, bool admissible = false,
                bool live = false, bool owns = true, bool displaced = false, bool slotsFree = true, int failed = 0, bool retryDue = false, bool realTime = true) =>
                MountedPreStartApproachPolicy.Decide(inSlot, realTime, true, started, false, perceived, forbidden, admissible, live, owns, displaced, slotsFree, failed, retryDue);
            // Queue-only acceptance behind a native prerequisite: native never ticks a queued command (as for an unmounted
            // attacker), so no movement; once promotion places the order in its slot, movement is admitted while the start waits.
            TestRunner.Equal(MountedPreStartApproachAction.None, Step(inSlot: false), "A queued order moved before promotion.");
            TestRunner.Equal(MountedPreStartApproachAction.Issue, Step(), "Movement was not admitted after promotion while the strike waited.");
            TestRunner.Equal(MountedPreStartApproachAction.Keep, Step(live: true), "The live carrier was not kept.");
            TestRunner.Equal(MountedPreStartApproachAction.Reissue, Step(live: true, displaced: true), "A moved target was not re-approached.");
            TestRunner.Equal(MountedPreStartApproachAction.Stop, Step(live: true, admissible: true), "The pair kept moving inside legal range.");
            TestRunner.Equal(MountedPreStartApproachAction.None, Step(admissible: true), "An in-range order moved.");
            TestRunner.True(Step(live: true, forbidden: true) == MountedPreStartApproachAction.Stop &&
                Step(live: true, perceived: false) == MountedPreStartApproachAction.Stop &&
                Step(live: true, realTime: false) == MountedPreStartApproachAction.Stop &&
                Step(live: true, started: true) == MountedPreStartApproachAction.Stop &&
                Step(live: true, inSlot: false) == MountedPreStartApproachAction.Stop, "The carrier outlived its lawful approach.");
            TestRunner.Equal(MountedPreStartApproachAction.Wait, Step(slotsFree: false), "A carrier was forced past a newer mount order.");
            TestRunner.Equal(MountedPreStartApproachAction.Stop, Step(live: true, owns: false), "A displaced carrier kept being driven.");
            TestRunner.Equal(MountedPreStartApproachAction.Wait, Step(failed: 1, retryDue: false), "A failed route was retried at once.");
            TestRunner.Equal(MountedPreStartApproachAction.Issue, Step(failed: 1, retryDue: true), "A failed route was not retried.");
            TestRunner.Equal(MountedPreStartApproachAction.None, Step(failed: MountedPreStartApproachPolicy.MaximumFailedRoutes, retryDue: true),
                "A failed route was requested without bound.");
        }

        private static void EngagementAttribution()
        {
            var record = new MountedEngagementRecord();
            // A proposal before the first combat update opens the window.
            record.ObserveProposal(100d, "kobold-far", "kobold-1", "[*kobold-far:2.4:0,kobold-1:1.6:0];bestScore=1", true, 4, null);
            TestRunner.True(record.IsOpen && record.OrderGeneration == 4, "An early proposal was dropped.");
            TestRunner.Equal("accepted-not-dispatched", record.FirstStop(), "An accepted order was misnamed.");
            record.ObserveDispatch(5);
            TestRunner.Equal("accepted-not-dispatched", record.FirstStop(), "Another order's dispatch was attributed.");
            // Unrelated movement finishes between samples, then a stationary automatic wrapper is admitted: the interval that
            // spans the change of owner credits nothing, and a wrapper without a carrier credits nothing.
            var wrapper = new object();
            record.ObserveDispatch(4);
            record.ObserveInterval(2.5f, null, null, false, false);
            record.ObserveInterval(1.5f, null, wrapper, false, false);
            record.ObserveInterval(0.0f, wrapper, wrapper, false, false);
            TestRunner.Equal("dispatched-not-moved", record.FirstStop(), "Unrelated or change-of-owner movement was credited to a stationary wrapper.");
            // A genuinely moving automatic carrier: the same exact carrier at both ends of each interval.
            var carrier = new object();
            record.ObserveInterval(0.4f, wrapper, carrier, false, false);
            record.ObserveInterval(0.6f, carrier, carrier, false, false);
            TestRunner.True(record.FirstStop() == "dispatched-moved" && System.Math.Abs(record.OwnDisplacementMetres - 0.6f) < 1e-5f,
                "The carrier's own travel was misattributed: " + record.OwnDisplacementMetres);
            TestRunner.True(!record.Due(102d) && record.Due(103d), "The report time was wrong.");
            var line = record.Report();
            // Review R5-F8: native's proposal and the pair's selection are reported separately.
            TestRunner.True(line.Contains("order=4") && line.Contains("proposed=kobold-far") && line.Contains("selected=kobold-1") &&
                line.Contains("nativeRanking=[*kobold-far"), "The report lost its attribution: " + line);
            // A player operation before any automatic dispatch: player movement is never automatic progress.
            var player = new MountedEngagementRecord();
            player.Open(0d);
            player.ObservePlayerOperation();
            player.ObserveInterval(3f, null, null, true, true);
            TestRunner.True(player.FirstStop() == "player-command" && player.OwnDisplacementMetres == 0f && player.PlayerDisplacementMetres == 3f,
                "Player movement was attributed to the automatic order.");
            var refused = new MountedEngagementRecord();
            refused.ObserveProposal(0d, "kobold-2", "kobold-2", null, false, -1, "hold-out-of-reach");
            TestRunner.Equal("refused:hold-out-of-reach", refused.FirstStop(), "A refusal lost its reason.");
            var none = new MountedEngagementRecord(); none.Open(0d);
            TestRunner.Equal("no-proposal", none.FirstStop(), "An absent proposal was misnamed.");
        }

        // Review R1-F11: the pair's tie-break among candidates native ranked equal (production NativeAttackTieBreak).
        private static void EqualRankTieBreak()
        {
            var far = new object(); var near = new object(); var outside = new object();
            NativeRankedCandidate C(object unit, float weight, float distance, bool eligible = true) =>
                new NativeRankedCandidate { Unit = unit, Weight = weight, Distance = distance, Eligible = eligible };
            // Review R5-F7: the same farther-first equal-rank proposal through the ranged (mounted archery) entry keeps the brain's choice.
            var rangedList = new List<NativeRankedCandidate> { C(far, 0f, 2.4f), C(near, 0f, 1.6f) };
            TestRunner.True(ReferenceEquals(far, NativeAttackTieBreak.Choose(rangedList, far, null, false)), "A ranged proposal was retargeted.");
            // Two equally eligible in-reach hostiles in farther-first order: native would take the farther; the pair takes the nearer.
            var ranked = new List<NativeRankedCandidate> { C(far, 0f, 2.4f), C(near, 0f, 1.6f), C(outside, -0.009f, 9f) };
            TestRunner.True(ReferenceEquals(near, NativeAttackTieBreak.Choose(ranked, far, null, true)), "The nearer equally ranked hostile was not chosen.");
            // The nearer one ineligible (unconscious, disliked, unattackable or invalid under Hold): native's proposal stands.
            ranked[1] = C(near, 0f, 1.6f, false);
            TestRunner.True(ReferenceEquals(far, NativeAttackTieBreak.Choose(ranked, far, null, true)), "An ineligible nearer hostile was chosen.");
            // Different weights (manual target, out-of-reach distance) are never reordered.
            var manual = new object();
            var withManual = new List<NativeRankedCandidate> { C(manual, 1000000f, 8f), C(near, 0f, 1.6f) };
            TestRunner.True(ReferenceEquals(manual, NativeAttackTieBreak.Choose(withManual, manual, null, true)), "A manual target was replaced.");
            // A live automatic order already on one of the tied candidates keeps it (no oscillation).
            ranked[1] = C(near, 0f, 1.6f);
            TestRunner.True(ReferenceEquals(far, NativeAttackTieBreak.Choose(ranked, far, far, true)) &&
                ReferenceEquals(far, NativeAttackTieBreak.Choose(ranked, near, far, true)), "A live order on a tied target oscillated.");
            TestRunner.True(ReferenceEquals(far, NativeAttackTieBreak.Choose(ranked, far, outside, true)) == false, "The tie-break ignored distance.");
        }

        private static void NativeTargetChoice()
        {
            NativeAttackCandidate C(string id, float distance, bool inRange = false, bool manual = false, bool excluded = false,
                bool unreachable = false, bool unconscious = false, bool disliked = false) =>
                new NativeAttackCandidate { Id = id, Distance = distance, InActionRange = inRange, ManualTarget = manual, Excluded = excluded,
                    Unreachable = unreachable, InLineOfSight = true, Unconscious = unconscious, Disliked = disliked };
            // Two eligible hostiles, both outside reach, in either list order: the nearer wins in real time.
            TestRunner.Equal("near", NativeAttackTargetChoiceModel.Choose(new[] { C("far", 12f), C("near", 7f) }, false, false), "The nearer eligible hostile lost.");
            TestRunner.Equal("near", NativeAttackTargetChoiceModel.Choose(new[] { C("near", 7f), C("far", 12f) }, false, false), "List order decided.");
            // The nearer one ineligible: unaware/dead/untargetable (excluded), or unreachable outside reach.
            TestRunner.Equal("far", NativeAttackTargetChoiceModel.Choose(new[] { C("far", 12f), C("near", 7f, excluded: true) }, false, false), "An excluded hostile was chosen.");
            TestRunner.Equal("far", NativeAttackTargetChoiceModel.Choose(new[] { C("far", 12f), C("near", 7f, unreachable: true) }, false, false), "An unreachable hostile was chosen.");
            // An unconscious or disliked nearer hostile is outranked by a conscious, liked farther one.
            TestRunner.Equal("far", NativeAttackTargetChoiceModel.Choose(new[] { C("far", 12f), C("near", 7f, unconscious: true) }, false, false), "The unconscious penalty was lost.");
            TestRunner.Equal("far", NativeAttackTargetChoiceModel.Choose(new[] { C("far", 12f), C("near", 7f, disliked: true) }, false, false), "The impatience penalty was lost.");
            // Without a manual target, a hostile already in reach outranks one that needs movement (InRange 1.0 versus 0.8).
            TestRunner.Equal("adjacent", NativeAttackTargetChoiceModel.Choose(new[] { C("near", 3f), C("adjacent", 1f, inRange: true) }, false, false),
                "Native's in-reach preference was not modelled.");
            // ManualAttackAiAction (BaseScore 10) keeps the player's manual target even beside an adjacent hostile.
            TestRunner.Equal("manual", NativeAttackTargetChoiceModel.Choose(new[] { C("manual", 9f, manual: true), C("adjacent", 1f, inRange: true) }, false, false),
                "An explicit farther target was replaced.");
            // A held rider's out-of-reach manual target is invalid; native falls back to the in-reach hostile.
            TestRunner.Equal("adjacent", NativeAttackTargetChoiceModel.Choose(new[] { C("manual", 9f, manual: true), C("adjacent", 1f, inRange: true) }, false, true),
                "A held rider chased its out-of-reach manual target.");
            // Both out of reach: the player's manual target is kept.
            TestRunner.Equal("manual", NativeAttackTargetChoiceModel.Choose(new[] { C("near", 5f), C("manual", 9f, manual: true) }, false, false), "The manual target was not kept.");
            // Hold (AiMovementForbidden): only an in-reach target in sight is valid.
            TestRunner.Equal(null, NativeAttackTargetChoiceModel.Choose(new[] { C("near", 5f) }, false, true), "A held rider chose an out-of-reach target.");
        }
    }
}
