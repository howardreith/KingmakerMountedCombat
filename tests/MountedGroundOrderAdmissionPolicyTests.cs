using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Behaviour tests for the production ground-order admission decision that
    // MountedCombatController.TryAdmitGroundCommand delegates to. The only substitute is the
    // live-fact seam: it answers the native facts and records, in order, every fact the decision
    // read and every pair-local cancel it performed.
    internal static class MountedGroundOrderAdmissionPolicyTests
    {
        private static readonly string[] RefusedModes =
        {
            "FullScreenUi", "EscMode", "Dialog", "Cutscene", "None", "GlobalMap", "Rest", "Kingdom",
            "GameOver", "BugReport", "KingdomSettlement", "CutsceneGlobalMap", "default", "pause", ""
        };

        public static void Register(TestRunner runner)
        {
            runner.Run("ground order: Default and Pause real-time orders are admitted after one pair cancel", RealTimeDefaultAndPauseAreAdmitted);
            runner.Run("ground order: Pause in turn-based combat is refused", TurnBasedPauseIsRefused);
            runner.Run("ground order: modal and world-changing modes are refused in RT and TB", UnsupportedModesAreRefused);
            runner.Run("ground order: no game refuses without reading turn facts", MissingGameIsRefused);
            runner.Run("ground order: save serialization refuses before any cancel or mode read", SaveSerializationRefusesBeforeCancel);
            runner.Run("ground order: a propagated overlay click is suppressed before save, cancel or mode", OverlayClickSuppressedFirst);
            runner.Run("ground order: turn-based rider-turn branches are unchanged", TurnBasedBranchesAreUnchanged);
            runner.Run("ground order: another pair's or an unpaired actor's order touches nothing of this pair", OtherActorsPassThroughUntouched);
            runner.Run("ground order: the selectable paired mount is this pair's input", PairedMountInputIsThisPair);
            runner.Run("ground order: dispositions keep their exact feedback, codes and native result", DispositionOutputsAreExact);
        }

        private sealed class Facts : IMountedGroundOrderFacts
        {
            public readonly List<string> Log = new List<string>();
            public bool OverlayClick;
            public bool Saving;
            public string Mode = "Default";
            public bool TurnBased;
            public bool PairedActivation = true;
            public bool CanMove = true;
            public bool RiderTurn = true;
            public bool Preparing;
            public bool Acting = true;
            public bool SlotIdle = true;

            public int Cancels => Count("cancel");
            public int Count(string entry) { var n = 0; foreach (var e in Log) if (e == entry) n++; return n; }

            public bool TryConsumePropagatedOverlayWorldClick() { Log.Add("overlay"); return OverlayClick; }
            public bool SaveSerializationSuspended { get { Log.Add("save"); return Saving; } }
            public void CancelPairCommands() { Log.Add("cancel"); }
            public string CurrentModeName { get { Log.Add("mode"); return Mode; } }
            public bool InTurnBasedCombat { get { Log.Add("tb"); return TurnBased; } }
            public bool PairedActivationEnabled { get { Log.Add("paired"); return PairedActivation; } }
            public bool CanMovePairedMount() { Log.Add("canMove"); return CanMove; }
            public bool CurrentTurnUnitIsRider { get { Log.Add("riderTurn"); return RiderTurn; } }
            public bool RiderTurnPreparing { get { Log.Add("preparing"); return Preparing; } }
            public bool RiderTurnActing { get { Log.Add("acting"); return Acting; } }
            public bool MountMoveSlotIdle { get { Log.Add("slot"); return SlotIdle; } }
        }

        private static MountedGroundOrderDisposition RiderOrder(Facts facts) =>
            MountedGroundOrderAdmissionPolicy.Decide(false, true, true, false, facts);

        private static int IndexOf(Facts facts, string entry) => facts.Log.IndexOf(entry);

        private static void RealTimeDefaultAndPauseAreAdmitted()
        {
            foreach (var mode in new[] { "Default", "Pause" })
            {
                var facts = new Facts { Mode = mode, TurnBased = false };
                var disposition = RiderOrder(facts);
                TestRunner.Equal(MountedGroundOrderDisposition.AdmitRealTime, disposition, mode + " RT ground order was not admitted.");
                TestRunner.True(MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), mode + " RT ground order did not reach the native queue.");
                TestRunner.Equal(1, facts.Cancels, mode + " RT ground order did not cancel the pair's current order exactly once.");
                TestRunner.True(IndexOf(facts, "save") < IndexOf(facts, "cancel") && IndexOf(facts, "cancel") < IndexOf(facts, "mode"),
                    mode + " RT ground order cancelled outside save-check-then-mode order: " + string.Join(",", facts.Log));
                foreach (var turnFact in new[] { "paired", "canMove", "riderTurn", "preparing", "acting", "slot" })
                    TestRunner.Equal(0, facts.Count(turnFact), mode + " RT ground order read turn-based fact " + turnFact + ".");
            }
        }

        private static void TurnBasedPauseIsRefused()
        {
            var facts = new Facts { Mode = "Pause", TurnBased = true };
            var disposition = RiderOrder(facts);
            TestRunner.Equal(MountedGroundOrderDisposition.RefuseGameMode, disposition, "Pause admitted a turn-based ground order.");
            TestRunner.True(!MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), "Refused TB Pause order reached the native queue.");
            TestRunner.Equal(0, facts.Count("slot") + facts.Count("canMove") + facts.Count("riderTurn"), "TB Pause refusal read rider-turn facts.");
            // TB Default with the same turn facts is admitted: the refusal is the mode alone.
            var active = new Facts { Mode = "Default", TurnBased = true };
            TestRunner.Equal(MountedGroundOrderDisposition.AdmitRiderTurnMovement, RiderOrder(active), "TB Default rider-turn order was refused.");
        }

        private static void UnsupportedModesAreRefused()
        {
            foreach (var turnBased in new[] { false, true })
            foreach (var mode in RefusedModes)
            {
                var facts = new Facts { Mode = mode, TurnBased = turnBased };
                var disposition = RiderOrder(facts);
                TestRunner.Equal(MountedGroundOrderDisposition.RefuseGameMode, disposition,
                    "Mode '" + mode + "' admitted a " + (turnBased ? "TB" : "RT") + " ground order.");
                TestRunner.True(!MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), "Mode '" + mode + "' let the native order run.");
                TestRunner.Equal(1, facts.Cancels, "Mode '" + mode + "' did not keep the single pre-mode pair cancel.");
                TestRunner.Equal(0, facts.Count("slot"), "Mode '" + mode + "' read the mount Move slot.");
            }
        }

        private static void MissingGameIsRefused()
        {
            var facts = new Facts { Mode = null, TurnBased = false };
            TestRunner.Equal(MountedGroundOrderDisposition.RefuseGameMode, RiderOrder(facts), "A ground order without a game was admitted.");
            TestRunner.Equal(0, facts.Count("tb"), "A ground order without a game read turn-based state.");
        }

        private static void SaveSerializationRefusesBeforeCancel()
        {
            foreach (var mode in new[] { "Default", "Pause", "Dialog" })
            foreach (var turnBased in new[] { false, true })
            {
                var facts = new Facts { Saving = true, Mode = mode, TurnBased = turnBased };
                var disposition = RiderOrder(facts);
                TestRunner.Equal(MountedGroundOrderDisposition.RefuseSaveSerialization, disposition, "A ground order was accepted during save serialization.");
                TestRunner.True(!MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), "A save-time ground order reached the native queue.");
                TestRunner.Equal(0, facts.Cancels, "Save serialization cancelled the pair's live command queue.");
                TestRunner.Equal("overlay,save", string.Join(",", facts.Log), "Save serialization refusal read facts beyond the save flag.");
            }
        }

        private static void OverlayClickSuppressedFirst()
        {
            var facts = new Facts { OverlayClick = true, Saving = true };
            var disposition = RiderOrder(facts);
            TestRunner.Equal(MountedGroundOrderDisposition.SuppressPropagatedOverlayClick, disposition, "A propagated overlay click became a world order.");
            TestRunner.True(!MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), "A propagated overlay click reached the native queue.");
            TestRunner.Equal("overlay", string.Join(",", facts.Log), "Overlay suppression read further facts or cancelled.");
        }

        private static void TurnBasedBranchesAreUnchanged()
        {
            var bools = new[] { false, true };
            foreach (var paired in bools)
            foreach (var canMove in bools)
            foreach (var riderTurn in bools)
            foreach (var preparing in bools)
            foreach (var acting in bools)
            foreach (var slotIdle in bools)
            {
                var facts = new Facts
                {
                    Mode = "Default", TurnBased = true, PairedActivation = paired, CanMove = canMove,
                    RiderTurn = riderTurn, Preparing = preparing, Acting = acting, SlotIdle = slotIdle
                };
                MountedGroundOrderDisposition expected;
                if (paired && !canMove) expected = MountedGroundOrderDisposition.RefusePairedMovementUnavailable;
                else if (!(riderTurn && (preparing || acting))) expected = MountedGroundOrderDisposition.RefuseWrongTurn;
                else if (!slotIdle) expected = MountedGroundOrderDisposition.RefuseMountMoveSlotBusy;
                else expected = MountedGroundOrderDisposition.AdmitRiderTurnMovement;
                var label = string.Format("paired={0} canMove={1} riderTurn={2} preparing={3} acting={4} slotIdle={5}",
                    paired, canMove, riderTurn, preparing, acting, slotIdle);
                TestRunner.Equal(expected, RiderOrder(facts), "TB branch changed: " + label);
                TestRunner.Equal(1, facts.Cancels, "TB ground order did not cancel exactly once: " + label);
                TestRunner.Equal(paired ? 1 : 0, facts.Count("canMove"), "Paired movement query count changed: " + label);
                TestRunner.Equal(expected == MountedGroundOrderDisposition.RefuseMountMoveSlotBusy ||
                    expected == MountedGroundOrderDisposition.AdmitRiderTurnMovement ? 1 : 0, facts.Count("slot"),
                    "Move slot was read outside the rider-turn admission: " + label);
            }
        }

        private static void OtherActorsPassThroughUntouched()
        {
            // Pair A sees an order for pair B's rider (or for an unpaired actor): neither its
            // rider nor its selectable paired mount. A reads nothing and cancels nothing.
            var pairA = new Facts { Mode = "Pause", TurnBased = false, OverlayClick = true };
            var pairB = new Facts { Mode = "Pause", TurnBased = false };
            var aDisposition = MountedGroundOrderAdmissionPolicy.Decide(false, true, false, false, pairA);
            TestRunner.Equal(MountedGroundOrderDisposition.PassThroughNotThisPair, aDisposition, "Pair A claimed pair B's ground order.");
            TestRunner.True(MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(aDisposition), "Pair A blocked another actor's native order.");
            TestRunner.Equal(0, pairA.Log.Count, "Pair A read facts, consumed its overlay click or cancelled for another pair's order: " + string.Join(",", pairA.Log));
            TestRunner.Equal(null, MountedGroundOrderAdmissionPolicy.RejectionCodes(aDisposition), "Pass-through rewrote pair A's rejection codes.");
            TestRunner.Equal(null, MountedGroundOrderAdmissionPolicy.Feedback(aDisposition, "Horse"), "Pass-through rewrote pair A's feedback.");
            TestRunner.True(pairA.OverlayClick, "Pass-through consumed pair A's pending overlay click.");

            var bDisposition = RiderOrder(pairB);
            TestRunner.Equal(MountedGroundOrderDisposition.AdmitRealTime, bDisposition, "Pair B's own paused RT order was refused.");
            TestRunner.Equal(1, pairB.Cancels, "Pair B did not cancel its own order exactly once.");
            TestRunner.Equal(0, pairA.Log.Count, "Pair B's admission touched pair A.");

            // Disposed or unmounted relationships pass every order through untouched, even the rider's.
            foreach (var shape in new[] { new[] { true, true }, new[] { false, false }, new[] { true, false } })
            {
                var facts = new Facts { Saving = true };
                TestRunner.Equal(MountedGroundOrderDisposition.PassThroughNotThisPair,
                    MountedGroundOrderAdmissionPolicy.Decide(shape[0], shape[1], true, true, facts),
                    "A disposed or unmounted relationship claimed a ground order.");
                TestRunner.Equal(0, facts.Log.Count, "A disposed or unmounted relationship read facts or cancelled.");
            }
        }

        private static void PairedMountInputIsThisPair()
        {
            var facts = new Facts { Mode = "Default", TurnBased = true };
            TestRunner.Equal(MountedGroundOrderDisposition.AdmitRiderTurnMovement,
                MountedGroundOrderAdmissionPolicy.Decide(false, true, false, true, facts), "Selected paired mount input was not this pair's order.");
            TestRunner.Equal(1, facts.Cancels, "Paired mount input did not cancel the pair's order exactly once.");
            var saving = new Facts { Saving = true };
            TestRunner.Equal(MountedGroundOrderDisposition.RefuseSaveSerialization,
                MountedGroundOrderAdmissionPolicy.Decide(false, true, false, true, saving), "Paired mount input bypassed save serialization.");
            TestRunner.Equal(0, saving.Cancels, "Paired mount input cancelled during save serialization.");
        }

        private static void DispositionOutputsAreExact()
        {
            Action<MountedGroundOrderDisposition, bool, MountedCombatRejectionCode?, string> check = (disposition, runs, code, feedback) =>
            {
                TestRunner.Equal(runs, MountedGroundOrderAdmissionPolicy.LetsNativeOrderRun(disposition), disposition + " native result changed.");
                var codes = MountedGroundOrderAdmissionPolicy.RejectionCodes(disposition);
                if (code == null)
                {
                    TestRunner.True(codes != null && codes.Length == 0 || disposition == MountedGroundOrderDisposition.PassThroughNotThisPair && codes == null ||
                        disposition == MountedGroundOrderDisposition.SuppressPropagatedOverlayClick && codes == null, disposition + " rejection codes changed.");
                }
                else
                {
                    TestRunner.True(codes != null && codes.Length == 1 && codes[0] == code.Value, disposition + " rejection code changed.");
                }
                TestRunner.Equal(feedback, MountedGroundOrderAdmissionPolicy.Feedback(disposition, "Horse"), disposition + " feedback changed.");
            };
            check(MountedGroundOrderDisposition.PassThroughNotThisPair, true, null, null);
            check(MountedGroundOrderDisposition.SuppressPropagatedOverlayClick, false, null, null);
            check(MountedGroundOrderDisposition.RefuseSaveSerialization, false, MountedCombatRejectionCode.WrongActionState,
                "A save is still being written; mounted actions resume when it finishes.");
            check(MountedGroundOrderDisposition.RefuseGameMode, false, MountedCombatRejectionCode.WrongActionState,
                "Mounted ground movement is available only in the active world view.");
            check(MountedGroundOrderDisposition.AdmitRealTime, true, null, null);
            check(MountedGroundOrderDisposition.RefusePairedMovementUnavailable, false, MountedCombatRejectionCode.WrongActionState,
                "The mount has no movement available in this paired activation.");
            check(MountedGroundOrderDisposition.RefuseWrongTurn, false, MountedCombatRejectionCode.WrongTurn,
                "Move the mounted pair during the rider's turn.");
            check(MountedGroundOrderDisposition.RefuseMountMoveSlotBusy, false, MountedCombatRejectionCode.AlreadyActiveCommand,
                "Mounted ground movement rejected: the Horse Move slot is not idle.");
            check(MountedGroundOrderDisposition.AdmitRiderTurnMovement, true, null,
                "Mounted rider-turn ground movement admitted; Horse owns pathfinding.");
            TestRunner.Equal(0, MountedGroundOrderAdmissionPolicy.RejectionCodes(MountedGroundOrderDisposition.AdmitRealTime).Length,
                "Admitted RT order kept stale rejection codes.");
        }
    }
}
