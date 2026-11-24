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
            runner.Run("R1-F8: only the player's Hold on the mount restricts automatic movement; own carrier writes are undone", MountHoldProvenance);
            runner.Run("R3-F1: real-time movement is admitted before strike readiness and released at range; native keeps the start", PreStartApproach);
            runner.Run("R3-F2: a dropped turn-based click is admitted only for an in-range strike or a granted native mount route", TurnBasedClickAdmission);
            runner.Run("R1-F11: engagement attribution follows one automatic order from proposal to its own displacement", EngagementAttribution);
            runner.Run("R1-F11: model of the traced native target comparison (two eligible, nearer ineligible, manual target)", NativeTargetChoice);
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
            var live = MountedAutomaticAttackPolicy.HasPlayerOperation(ownSlot, 0) || order.Superseded(21);
            TestRunner.True(MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false, false, true, live) == null,
                "The order's own carrier retired its order.");
            // A newer player command put directly into a mount slot while the old attack waits: on the update an actor becomes
            // strike-ready, the controller's retirement reads it before any dispatch.
            order.ObserveAccepted(new object(), PlayerMove, MountedAcceptedWorkRoute.Slot, "mount:UnitMoveTo");
            var playerSlot = new List<MountedAutomaticSlotFacts> { new MountedAutomaticSlotFacts() };
            live = MountedAutomaticAttackPolicy.HasPlayerOperation(playerSlot, 0) || order.Superseded(21);
            TestRunner.Equal("newer player operation", MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false, false, true, live),
                "A newer mount-slot order did not retire the waiting order.");
            // The same for a player's real-time click order.
            TestRunner.Equal("newer player operation", MountedAutomaticAttackPolicy.RetireReason(true, false, true, true, true, false, false, true, live),
                "A newer mount-slot order did not retire the player's waiting order.");
        }

        private static void MountHoldProvenance()
        {
            var hold = new MountedHoldProvenance();
            // A Hold flag the player never set (an earlier carrier's write left in a save): automatic movement stays lawful.
            TestRunner.True(!MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHeldByPlayer(true)),
                "A non-player Hold restricted automatic movement.");
            // The player holds the mount (mount selected), the rider unheld: automatic movement is forbidden at admission.
            hold.ObservePlayerHoldInput(true);
            TestRunner.True(MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHeldByPlayer(true)),
                "A held mount with an unheld rider moved automatically.");
            // A Hold change before promotion or during travel: the next update's retirement sees it while the carrier is live.
            TestRunner.Equal("hold out of reach", MountedAutomaticAttackPolicy.RetireReason(true, true, true, true, true, false,
                MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, hold.MountHeldByPlayer(true)), false, false),
                "A Hold during travel did not stop the automatic approach.");
            // Native clears the Hold (a new order, Stop): the restriction ends with it.
            hold.Observe(false);
            TestRunner.True(!hold.MountHeldByPlayer(true), "A cleared Hold kept restricting.");
            hold.ObservePlayerHoldInput(false);
            TestRunner.True(!hold.PlayerHoldObserved, "A Hold input without the mount selected was attributed to the mount.");
            hold.ObservePlayerHoldInput(true); hold.ObservePlayerStopInput(true);
            TestRunner.True(!hold.PlayerHoldObserved, "Stop did not clear the player's mount Hold.");
            // Unheld control and the rider's own Hold / completed player move.
            TestRunner.True(!MountedHoldProvenance.AutomaticMovementForbidden(false, false, true, false), "An unheld pair was restricted.");
            TestRunner.True(MountedHoldProvenance.AutomaticMovementForbidden(true, false, true, false) &&
                MountedHoldProvenance.AutomaticMovementForbidden(false, true, true, false) &&
                MountedHoldProvenance.AutomaticMovementForbidden(false, false, false, false), "Rider Hold, a completed player move or an immobile mount lost its effect.");
            // The native Hold writes: UnitMoveTo.OnAction's write is undone only for the pair's own carrier; Run keeps the
            // mount's Hold only for an automatic order's own move.
            TestRunner.True(MountedHoldProvenance.RestoreHoldAfterMoveAction(true, false, true) &&
                !MountedHoldProvenance.RestoreHoldAfterMoveAction(false, false, true) &&
                !MountedHoldProvenance.RestoreHoldAfterMoveAction(true, true, true), "The carrier Hold restore was misapplied.");
            TestRunner.True(MountedHoldProvenance.NativeRunKeepsMountHold(true, true) && !MountedHoldProvenance.NativeRunKeepsMountHold(true, false) &&
                !MountedHoldProvenance.NativeRunKeepsMountHold(false, true), "Run's Hold keep was misapplied.");
        }

        private static void PreStartApproach()
        {
            // Dispatch before readiness: out of mounted reach, Standard only on cooldown, movement lawful.
            bool Early(bool forbidden = false, bool inReach = false, bool riderReady = false, bool mountReady = false, bool restricted = false,
                bool canAct = true, bool melee = true, bool riderOrder = true, bool realTime = true) =>
                MountedPreStartApproachPolicy.DispatchBeforeStrikeReadiness(realTime, riderOrder, melee, forbidden, inReach, riderReady, mountReady, restricted, canAct);
            TestRunner.True(Early(), "An out-of-reach order waited for strike readiness before moving.");
            TestRunner.True(!Early(inReach: true) && !Early(riderReady: true) && !Early(mountReady: true),
                "Dispatch before readiness replaced ordinary dispatch.");
            TestRunner.True(!Early(restricted: true) && !Early(canAct: false) && !Early(forbidden: true) && !Early(melee: false) &&
                !Early(riderOrder: false) && !Early(realTime: false), "A restricted, held, ranged, mount or turn-based order moved early.");
            MountedPreStartApproachAction Step(bool started = false, bool perceived = true, bool forbidden = false, bool admissible = false,
                bool live = false, bool owns = true, bool displaced = false, bool slotsFree = true, int failed = 0, bool retryDue = false, bool realTime = true) =>
                MountedPreStartApproachPolicy.Decide(realTime, true, started, false, perceived, forbidden, admissible, live, owns, displaced, slotsFree, failed, retryDue);
            // Movement is admitted while native still refuses the start (cooldown, initiative wait).
            TestRunner.Equal(MountedPreStartApproachAction.Issue, Step(), "Movement was not admitted while the strike waited.");
            TestRunner.Equal(MountedPreStartApproachAction.Keep, Step(live: true), "The live carrier was not kept.");
            TestRunner.Equal(MountedPreStartApproachAction.Reissue, Step(live: true, displaced: true), "A moved target was not re-approached.");
            // At legal range the pair stops and waits for native to start the attack (one attack, native expenditure).
            TestRunner.Equal(MountedPreStartApproachAction.Stop, Step(live: true, admissible: true), "The pair kept moving inside legal range.");
            TestRunner.Equal(MountedPreStartApproachAction.None, Step(admissible: true), "An in-range order moved.");
            // Hold, a hidden target, turn-based mode, or the command leaving its unstarted phase release the exact carrier.
            TestRunner.True(Step(live: true, forbidden: true) == MountedPreStartApproachAction.Stop &&
                Step(live: true, perceived: false) == MountedPreStartApproachAction.Stop &&
                Step(live: true, realTime: false) == MountedPreStartApproachAction.Stop &&
                Step(live: true, started: true) == MountedPreStartApproachAction.Stop, "The carrier outlived its lawful approach.");
            // A newer order holds the mount: no carrier is forced in; a carrier that lost its slot is released.
            TestRunner.Equal(MountedPreStartApproachAction.Wait, Step(slotsFree: false), "A carrier was forced past a newer mount order.");
            TestRunner.Equal(MountedPreStartApproachAction.Stop, Step(live: true, owns: false), "A displaced carrier kept being driven.");
            // Failed routes: bounded retries at the retry time, then the started command's own bounded approach decides.
            TestRunner.Equal(MountedPreStartApproachAction.Wait, Step(failed: 1, retryDue: false), "A failed route was retried at once.");
            TestRunner.Equal(MountedPreStartApproachAction.Issue, Step(failed: 1, retryDue: true), "A failed route was not retried.");
            TestRunner.Equal(MountedPreStartApproachAction.None, Step(failed: MountedPreStartApproachPolicy.MaximumFailedRoutes, retryDue: true),
                "A failed route was requested without bound.");
        }

        private static void TurnBasedClickAdmission()
        {
            PairedAttackClickAdmission Decide(out string reason, bool ignored = true, bool addressable = true, bool attack = true,
                bool inRange = false, bool route = true, float routeMetres = 1.2f, float granted = 1.5f, bool endpoint = true,
                bool melee = true, bool selected = true, bool empty = true, bool current = true, bool terrain = false, bool ability = false) =>
                PairedAttackClickAdmissionPolicy.Decide(ignored, true, addressable, selected, empty, current, terrain, ability, melee, attack,
                    inRange, route, routeMetres, granted, endpoint, out reason);
            string why;
            // A legal five-foot step: the mount's remaining step covers its native route to a point that admits the strike.
            TestRunner.Equal(PairedAttackClickAdmission.AdmitRoute, Decide(out why, routeMetres: 1.2f, granted: 1.5f), "A legal step was refused.");
            // Ordinary movement within the mount's granted Move.
            TestRunner.Equal(PairedAttackClickAdmission.AdmitRoute, Decide(out why, routeMetres: 6.4f, granted: 9.1f), "Ordinary movement was refused.");
            // Already in mounted reach after the mount's movement is spent: no locomotion grant is involved.
            TestRunner.Equal(PairedAttackClickAdmission.AdmitInRange, Decide(out why, inRange: true, route: false, routeMetres: float.NaN, granted: 0f),
                "An in-range attack after spent movement was refused.");
            // Insufficient movement and blocked routes keep native's refusal (the previous order stays).
            TestRunner.Equal(PairedAttackClickAdmission.NativeDecision, Decide(out why, routeMetres: 6.4f, granted: 1.5f), "An over-budget route was admitted.");
            TestRunner.Equal("insufficient-movement", why, "The over-budget reason was lost.");
            TestRunner.Equal(PairedAttackClickAdmission.NativeDecision, Decide(out why, route: false), "A failed route was admitted.");
            TestRunner.Equal(PairedAttackClickAdmission.NativeDecision, Decide(out why, endpoint: false), "A route ending out of reach or sight was admitted.");
            TestRunner.Equal(PairedAttackClickAdmission.NativeDecision, Decide(out why, routeMetres: 1.2f, granted: 0f), "Spent movement admitted a route.");
            // Not the rider's lawful attack input: native decides (another initiative arrangement's actor, refused Standard, a
            // non-attack prediction, stale predictions, terrain, a selected ability, a busy rider).
            TestRunner.True(Decide(out why, addressable: false) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, attack: false) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, melee: false) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, current: false) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, terrain: true) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, ability: true) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, empty: false) == PairedAttackClickAdmission.NativeDecision &&
                Decide(out why, selected: false) == PairedAttackClickAdmission.NativeDecision, "Unrelated input was admitted.");
            // A click native accepted is never touched.
            TestRunner.Equal(PairedAttackClickAdmission.NativeDecision, Decide(out why, ignored: false, inRange: true), "An accepted click was altered.");
        }

        private static void EngagementAttribution()
        {
            var record = new MountedEngagementRecord();
            // A proposal before the first combat update opens the window.
            record.ObserveProposal(100d, "kobold-1", "[*kobold-1:6:-0.006];bestScore=0.8", true, 4, null);
            TestRunner.True(record.IsOpen && record.OrderGeneration == 4, "An early proposal was dropped.");
            TestRunner.Equal("accepted-not-dispatched", record.FirstStop(), "An accepted order was misnamed.");
            // Movement from an unrelated (non-own) source is not counted as the order's progress.
            record.ObserveDisplacement(2f, false, false);
            record.ObserveDispatch(5);
            TestRunner.Equal("accepted-not-dispatched", record.FirstStop(), "Another order's dispatch or movement was attributed.");
            record.ObserveDispatch(4);
            TestRunner.Equal("dispatched-not-moved", record.FirstStop(), "A stationary dispatch was counted as moving.");
            record.ObserveDisplacement(1f, true, false);
            TestRunner.Equal("dispatched-moved", record.FirstStop(), "The order's own displacement was missed.");
            TestRunner.True(!record.Due(102d) && record.Due(103d), "The report time was wrong.");
            var line = record.Report();
            TestRunner.True(line.Contains("order=4") && line.Contains("nativeRanking=[*kobold-1") && line.Contains("ownDisplacement=1"),
                "The report lost its attribution: " + line);
            // A player operation before any automatic dispatch: player movement is never automatic progress.
            var player = new MountedEngagementRecord();
            player.Open(0d);
            player.ObservePlayerOperation();
            player.ObserveDisplacement(3f, false, true);
            TestRunner.True(player.FirstStop() == "player-command" && player.OwnDisplacementMetres == 0f && player.PlayerDisplacementMetres == 3f,
                "Player movement was attributed to the automatic order.");
            var refused = new MountedEngagementRecord();
            refused.ObserveProposal(0d, "kobold-2", null, false, -1, "hold-out-of-reach");
            TestRunner.Equal("refused:hold-out-of-reach", refused.FirstStop(), "A refusal lost its reason.");
            var none = new MountedEngagementRecord(); none.Open(0d);
            TestRunner.Equal("no-proposal", none.FirstStop(), "An absent proposal was misnamed.");
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
