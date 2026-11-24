using System;
using System.Collections.Generic;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-07 (review R1-F13): the production handle binding (HorseStrikeHandleBinding, called by
    // HorsePrimaryAttackAnimationAdapter.SupplyExact) and the production strike reader (HorseStrikeReader, called by
    // MountedPairAttackCommand.ObserveHorseStrike), driven through fakes of their Unity boundaries. The fake command record
    // keeps MountedPairAttackCommand's invariants (a second Record or an inexact Refresh throws), so a passing case also shows
    // the binding never asks for one. Genuine Kingmaker handle/action objects drive the real-type boundary in
    // scripts/Test-MountedAlpha2BiteContract.ps1. Nothing here proves what was rendered.
    internal static class HorseStrikeHandleBindingTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("R1-F13: the exact Horse adopts the stock-created Bite handle once", StockAdoptionOnce);
            runner.Run("R1-F13: a later stock handle of the same action refreshes the record; a different action is refused", RefreshSameActionOnly);
            runner.Run("R1-F13: a missing stock handle gets exactly one supplied handle of the single matching action", MissingHandleSupplied);
            runner.Run("R1-F13: zero or two matching actions, a failed creation or a foreign handle assign and record nothing", RejectionsAssignNothing);
            runner.Run("R1-F13: a hoof strike adopts the hoof action, never the bite", HoofControl);
            runner.Run("R1-F13: a non-Horse or other-actor strike is not bound", NotExactContext);
            runner.Run("R1-F13: the strike reader reports the handle bound to the planned attack (adopted, refreshed, supplied, missing, hoof)", ReaderReflectsBoundHandles);
        }

        internal sealed class FakeAction
        {
            public string Name, Type;
            public override string ToString() => Name;
        }

        internal sealed class FakeHandle
        {
            public FakeAction Action;
            public object Manager;
            public bool Started, Acted, Interrupted;
            public float Speed = 1f;
            public int Variant = 0, Variants = 1;
            public string Clip, State;
            public float Weight;
        }

        // One planned native strike: the attack's weapon and current handle, the Horse manager and its action set, and the
        // pair command's telemetry record.
        internal sealed class Strike
        {
            public bool Exact = true;
            public string Weapon = "Bite1d4", Expected = "Bite";
            public FakeHandle Handle;
            public object Manager = new object();
            public bool ManagerAlive = true;
            public List<FakeAction> Actions = new List<FakeAction>();
            public FakeHandle Recorded;
            public FakeAction RecordedAction;
            public string RecordedSource;
            public int Records, Refreshes, Creates, Assigns;
            public bool CreateFails;

            public HorseStrikeHandleBindingResult<FakeHandle, FakeAction> Bind() => HorseStrikeHandleBinding.Bind(new Boundary(this));
        }

        private sealed class Boundary : IHorseStrikeHandleBoundary<FakeHandle, FakeAction>
        {
            private readonly Strike strike;
            public Boundary(Strike strike) { this.strike = strike; }
            public bool ExactContext => strike.Exact;
            public FakeHandle CurrentHandle => strike.Handle;
            public bool IsRecorded(FakeHandle handle) => handle != null && ReferenceEquals(strike.Recorded, handle);
            public FakeAction RecordedAction => strike.RecordedAction;
            public bool HasAnyRecorded => strike.Recorded != null;
            public FakeAction StockSpecialAction(FakeHandle handle) => handle.Action;
            public bool HandleUsesHorseManager(FakeHandle handle) => strike.ManagerAlive && ReferenceEquals(handle.Manager, strike.Manager);
            public bool ExpectedTypeIsNone => strike.Expected == "None";
            public bool MatchesExpectedType(FakeAction action) => action.Type == strike.Expected;
            public IList<FakeAction> SpecialAttackActions => strike.ManagerAlive ? strike.Actions : new List<FakeAction>();
            public FakeHandle CreateHandle(FakeAction action)
            {
                strike.Creates++;
                return strike.CreateFails ? null : new FakeHandle { Action = action, Manager = strike.Manager };
            }
            public void AssignHandle(FakeHandle handle) { strike.Assigns++; strike.Handle = handle; }
            public void Record(FakeHandle handle, FakeAction action, string source)
            {
                if (strike.Recorded != null) throw new InvalidOperationException("duplicate record");
                strike.Recorded = handle; strike.RecordedAction = action; strike.RecordedSource = source; strike.Records++;
            }
            public void Refresh(FakeHandle handle, FakeAction action)
            {
                if (strike.Recorded == null || ReferenceEquals(strike.Recorded, handle) || !ReferenceEquals(strike.RecordedAction, action))
                    throw new InvalidOperationException("inexact refresh");
                strike.Recorded = handle; strike.RecordedSource = HorseStrikeHandleBinding.StockCreated; strike.Refreshes++;
            }
        }

        private static readonly FakeAction BiteAction = new FakeAction { Name = "HorseAnimationSet_Bite", Type = "Bite" };
        private static readonly FakeAction HoofAction = new FakeAction { Name = "HorseAnimationSet_Hoof", Type = "Hoof" };

        private static Strike HorseStrike(FakeHandle handle = null)
        {
            var strike = new Strike();
            strike.Actions.Add(BiteAction);
            strike.Actions.Add(HoofAction);
            if (handle != null) { handle.Manager = strike.Manager; strike.Handle = handle; }
            return strike;
        }

        private static void StockAdoptionOnce()
        {
            var stock = new FakeHandle { Action = BiteAction };
            var strike = HorseStrike(stock);
            var first = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, first.Outcome, "The stock Bite handle was not adopted.");
            TestRunner.True(ReferenceEquals(strike.Recorded, stock) && strike.RecordedSource == HorseStrikeHandleBinding.StockCreated &&
                ReferenceEquals(strike.Handle, stock) && strike.Assigns == 0 && strike.Creates == 0, "Adoption replaced or recreated the stock handle.");
            // The native creation path calls the postfix again for the same planned attack: still one record.
            var again = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.AlreadyRecorded, again.Outcome, "The adopted handle was bound twice.");
            TestRunner.Equal(1, strike.Records, "The stock handle was recorded more than once.");
        }

        private static void RefreshSameActionOnly()
        {
            var strike = HorseStrike(new FakeHandle { Action = BiteAction });
            strike.Bind();
            var later = new FakeHandle { Action = BiteAction, Manager = strike.Manager };
            strike.Handle = later;
            var refreshed = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.Refreshed, refreshed.Outcome, "A later stock handle of the same action did not refresh the record.");
            TestRunner.True(ReferenceEquals(strike.Recorded, later) && strike.Records == 1 && strike.Refreshes == 1, "The refresh recorded a second handle.");
            // A later handle that plays a different SpecialAttack action (the hoof) is refused, and the record keeps the bite.
            strike.Expected = "Hoof";
            strike.Handle = new FakeHandle { Action = HoofAction, Manager = strike.Manager };
            var refused = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedRefreshAction, refused.Outcome, "A refresh to a different action was accepted.");
            TestRunner.True(ReferenceEquals(strike.Recorded, later) && strike.Refreshes == 1, "A refused refresh changed the record.");
        }

        private static void MissingHandleSupplied()
        {
            var strike = HorseStrike();
            var supplied = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.Supplied, supplied.Outcome, "A missing stock handle was not supplied.");
            TestRunner.True(strike.Creates == 1 && strike.Assigns == 1 && strike.Records == 1 && ReferenceEquals(strike.Handle, supplied.Handle) &&
                ReferenceEquals(supplied.Handle.Action, BiteAction) && strike.RecordedSource == HorseStrikeHandleBinding.KmcSupplied,
                "The fallback did not assign exactly one handle of the bite action.");
            var again = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.AlreadyRecorded, again.Outcome, "The supplied handle was supplied again.");
            TestRunner.True(strike.Creates == 1 && strike.Assigns == 1, "The fallback ran twice for one strike.");
        }

        private static void RejectionsAssignNothing()
        {
            var none = HorseStrike(); none.Actions.Clear();
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedCandidateCount, none.Bind().Outcome, "Zero matching actions were not refused.");
            var two = HorseStrike(); two.Actions.Add(new FakeAction { Name = "SecondBite", Type = "Bite" });
            var twoResult = two.Bind();
            TestRunner.True(twoResult.Outcome == HorseStrikeHandleOutcome.RejectedCandidateCount && twoResult.CandidateCount == 2, "Two matching actions were not refused.");
            var failing = HorseStrike(); failing.CreateFails = true;
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedCreateFailed, failing.Bind().Outcome, "A failed creation was not refused.");
            var foreign = HorseStrike(new FakeHandle { Action = BiteAction }); foreign.Handle.Manager = new object();
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, foreign.Bind().Outcome, "A handle from another manager was adopted.");
            var stranger = HorseStrike(new FakeHandle { Action = new FakeAction { Name = "Unlisted", Type = "Bite" } });
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, stranger.Bind().Outcome, "An action outside the Horse set was adopted.");
            var dead = HorseStrike(new FakeHandle { Action = BiteAction }); dead.ManagerAlive = false;
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, dead.Bind().Outcome, "A handle was adopted without a live manager.");
            var untyped = HorseStrike(new FakeHandle { Action = BiteAction }); untyped.Expected = "None";
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, untyped.Bind().Outcome, "A stock handle was adopted for a weapon with no special type.");
            foreach (var strike in new[] { none, two, failing, foreign, stranger, dead, untyped })
                TestRunner.True(strike.Records == 0 && strike.Assigns == 0 && strike.Refreshes == 0, "A refused binding recorded or assigned a handle.");
        }

        private static void HoofControl()
        {
            var stockHoof = HorseStrike(new FakeHandle { Action = HoofAction }); stockHoof.Weapon = "Hoof1d3"; stockHoof.Expected = "Hoof";
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, stockHoof.Bind().Outcome, "The stock hoof handle was not adopted.");
            TestRunner.True(ReferenceEquals(stockHoof.RecordedAction, HoofAction), "The hoof strike recorded the bite action.");
            var bitePlayedForHoof = HorseStrike(new FakeHandle { Action = BiteAction }); bitePlayedForHoof.Expected = "Hoof";
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, bitePlayedForHoof.Bind().Outcome, "A bite handle was adopted for a hoof strike.");
            var missingHoof = HorseStrike(); missingHoof.Expected = "Hoof";
            var supplied = missingHoof.Bind();
            TestRunner.True(supplied.Outcome == HorseStrikeHandleOutcome.Supplied && ReferenceEquals(supplied.Handle.Action, HoofAction),
                "The missing hoof handle was not supplied from the hoof action.");
        }

        private static void NotExactContext()
        {
            var strike = HorseStrike(new FakeHandle { Action = BiteAction }); strike.Exact = false;
            var result = strike.Bind();
            TestRunner.True(result.Outcome == HorseStrikeHandleOutcome.NotExact && strike.Records == 0 && strike.Assigns == 0 && strike.Creates == 0,
                "A strike outside the exact Horse context was bound.");
            var missing = HorseStrike(); missing.Exact = false;
            TestRunner.True(missing.Bind().Outcome == HorseStrikeHandleOutcome.NotExact && missing.Creates == 0, "A non-Horse strike received a handle.");
        }

        private sealed class Source : IHorseStrikeSource<FakeHandle>
        {
            private readonly Strike strike;
            public Source(Strike strike) { this.strike = strike; }
            public string WeaponName => strike.Weapon;
            public string SpecialType => strike.Expected;
            public FakeHandle Handle => strike.Handle;
            public string ActionName(FakeHandle handle) => handle.Action?.Name;
            public bool IsStarted(FakeHandle handle) => handle.Started;
            public bool IsActed(FakeHandle handle) => handle.Acted;
            public bool IsInterrupted(FakeHandle handle) => handle.Interrupted;
            public float SpeedScale(FakeHandle handle) => handle.Speed;
            public int Variant(FakeHandle handle) => handle.Variant;
            public int VariantsCount(FakeHandle handle) => handle.Variants;
            public string ActiveClip(FakeHandle handle) => handle.Clip;
            public float ActiveWeight(FakeHandle handle) => handle.Weight;
            public string ActiveState(FakeHandle handle) => handle.State;
        }

        private static HorseStrikeRecord Read(Strike strike, int index = 1) => HorseStrikeReader.Read(new Source(strike), "horse-1", index, 3, "kobold");

        private static void ReaderReflectsBoundHandles()
        {
            var stock = new FakeHandle { Action = BiteAction, Started = true, Acted = true, Speed = 2.4f, Clip = "BiteAttack", Weight = 0.8f, State = "Playing" };
            var strike = HorseStrike(stock);
            strike.Bind();
            var adopted = Read(strike);
            TestRunner.True(ReferenceEquals(adopted.HandleIdentity, stock) && adopted.Action == "HorseAnimationSet_Bite" && adopted.ActiveClip == "BiteAttack" &&
                adopted.Format().Contains("handle=started,acted") && adopted.Format().Contains("speedScale=2.4"), "The record did not reflect the adopted handle.");
            var later = new FakeHandle { Action = BiteAction, Manager = strike.Manager, Started = true, Interrupted = true, Clip = "BiteAttack", State = "TransitioningOut" };
            strike.Handle = later;
            strike.Bind();
            var refreshed = Read(strike);
            TestRunner.True(ReferenceEquals(refreshed.HandleIdentity, later) && refreshed.Interrupted && refreshed.Format().Contains("handle=started,not-acted,interrupted"),
                "The record did not follow the refreshed handle.");
            var missing = HorseStrike(); missing.CreateFails = true;
            missing.Bind();
            var none = Read(missing);
            TestRunner.True(!none.HasHandle && none.HandleIdentity == null && none.Format().Contains("handle=none") && !none.Format().Contains("activeClip"),
                "A strike with no handle was not reported as handle=none.");
            var fallback = HorseStrike();
            var supplied = fallback.Bind();
            var suppliedRecord = Read(fallback);
            TestRunner.True(ReferenceEquals(suppliedRecord.HandleIdentity, supplied.Handle) && suppliedRecord.Format().Contains("handle=not-started,not-acted"),
                "The record did not reflect the supplied handle.");
            var hoof = HorseStrike(new FakeHandle { Action = HoofAction, Started = true, Acted = true, Clip = "Left_Hoof_Attack", Weight = 1f, State = "Playing" });
            hoof.Weapon = "Hoof1d3"; hoof.Expected = "Hoof";
            hoof.Bind();
            var hoofRecord = Read(hoof, 2);
            TestRunner.True(hoofRecord.Format().Contains("special=Hoof") && hoofRecord.Format().Contains("activeClip=Left_Hoof_Attack") &&
                hoofRecord.Format().Contains("index=2/3") && hoofRecord.Action == "HorseAnimationSet_Hoof", "The hoof control was not distinguishable.");
        }
    }
}
