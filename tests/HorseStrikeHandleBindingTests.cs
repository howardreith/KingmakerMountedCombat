using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // MMR-07 (review R1-F13): the production handle binding (HorseStrikeHandleBinding, called by
    // HorsePrimaryAttackAnimationAdapter.SupplyExact through SupplyContained) and the production strike reader and terminal
    // description (HorseStrikeReader, HorseBiteObservation, HorseBiteTerminal, called by MountedPairAttackCommand), driven
    // through fakes of their Unity boundaries. The fake command record keeps MountedPairAttackCommand's invariants (a second
    // Record or an inexact Refresh throws), so a passing case also shows the binding never asks for one. Genuine Kingmaker
    // handle/action/slot/blueprint objects drive the real-type boundary and the Bite-limb proof in
    // scripts/Test-MountedAlpha2BiteContract.ps1. Nothing here proves what was rendered.
    internal static class HorseStrikeHandleBindingTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("R1-F13: the exact Horse adopts the stock-created Bite handle once", StockAdoptionOnce);
            runner.Run("R1-F13: a later stock handle of the same Bite action refreshes the record; a different action is refused", RefreshSameActionOnly);
            runner.Run("R1-F13: a missing stock handle gets exactly one supplied handle of the single Bite action", MissingHandleSupplied);
            runner.Run("R1-F13: zero or two Bite actions, a failed creation or a foreign handle assign and record nothing", RejectionsAssignNothing);
            runner.Run("R1-F13: a non-Horse or other-actor strike is not bound", NotExactContext);
            runner.Run("R1-F13: the strike reader reports the handle bound to the planned attack (adopted, refreshed, supplied, missing, hoof)", ReaderReflectsBoundHandles);
            runner.Run("R4-F7: a strike without a handle after an earlier binding gets no fallback, no second record and no assignment", MissingAfterRecordedLeavesStrikeIntact);
            runner.Run("PR-5 R1-F1: a hoof never populates the Bite registration or receives its fallback (with and without a stock handle, untyped weapon)", HoofNeverBindsBite);
            runner.Run("PR-5 R1-F2: a throwing outcome report and a throwing fault report never escape; the planned slot is left as bound", ReportingContained);
            runner.Run("PR-5 R1-F3: a record fault after assignment restores the exact prior slot and records nothing; the retry supplies once", RecordFaultLeavesNoPartialFallback);
            runner.Run("PR-5 R1-F4: the terminal Bite fields describe the Bite observed at its strike, not a superseded registration", TerminalDescribesObservedBite);
            runner.Run("PR-5 R1-F5: the strike line names the native slot; two hooves sharing a blueprint differ, repeated strikes of one limb agree", StrikeSlotIdentity);
            runner.Run("PR-5 R1-F6: an unreadable playback or handle read keeps one identifiable strike line and states the gap", UnreadableObservationKeepsRecord);
            runner.Run("PR-5 R1-F7: the missing-handle report is once per command, held by the command", MissingReportOncePerCommand);
            runner.Run("PR-5 R1-F11: terminal and strike numbers are invariant under a decimal-comma culture", InvariantCultureFormatting);
            runner.Run("PR-5 R2-F2: a failing plan, limb or handle read after an observed Bite never escapes; one line names the Horse; the observation is explicitly unreadable", ObservationStepContainsFailures);
            runner.Run("PR-5 R2-F4: a failing variant-count read keeps the readable handle facts and the BiteAttack playback in the one strike line", HandleMetadataFaultKeepsPlayback);
            runner.Run("PR-5 R2-F3: the minimal terminal outcome carries the observed Bite disposition (one callback); detailed and non-Horse controls", MinimalTerminalKeepsBite);
            runner.Run("PR-5 R1-F4: the structured playback projection follows the observed Bite; a handle-less Bite after adoption is rejected; Bite then hoof is accepted", StructuredPlaybackFollowsObservation);
            runner.Run("PR-5 R2-F2: a missing plan or missing limb evidence after a Bite is an explicit unavailable observation with one line; a known hoof keeps the Bite", MissingInputsAreUnavailable);
            runner.Run("PR-5 R3-F4: a registration refresh before the command ends keeps the source captured with the observed Bite; an unregistered handle stays unrecorded", ObservedSourceSurvivesRefresh);
            runner.Run("PR-5 R3-F3: an unreadable observed handle fact is rejected by the one complete playback decision; the readable positive passes", UnreadableFactsRejected);
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
            public bool Started, Acted, Interrupted, Finished;
            public float Speed = 1f;
            public int Variant = 0, Variants = 1;
            public string Clip, State;
            public float Weight;
            public bool ClipThrows, FactsThrow, VariantsThrow;
        }

        // One planned native strike: whether its context is the exact Horse Bite limb (the adapter's proof), its weapon and
        // current handle, the Horse manager and its action set, and the pair command's telemetry record and flag.
        internal sealed class Strike
        {
            public bool Exact = true;
            public string Weapon = "Bite1d4", Special = "Bite", Slot = "limb0";
            public FakeHandle Handle;
            public object Manager = new object();
            public bool ManagerAlive = true;
            public List<FakeAction> Actions = new List<FakeAction>();
            public FakeHandle Recorded;
            public FakeAction RecordedAction;
            public string RecordedSource;
            public int Records, Refreshes, Creates, Assigns, NonNullAssigns;
            public bool CreateFails, RecordFailsOnce, MissingReported;

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
            public bool IsBiteAction(FakeAction action) => action.Type == "Bite";
            public IList<FakeAction> SpecialAttackActions => strike.ManagerAlive ? strike.Actions : new List<FakeAction>();
            public FakeHandle CreateHandle(FakeAction action)
            {
                strike.Creates++;
                return strike.CreateFails ? null : new FakeHandle { Action = action, Manager = strike.Manager };
            }
            public void AssignHandle(FakeHandle handle) { strike.Assigns++; if (handle != null) strike.NonNullAssigns++; strike.Handle = handle; }
            // As MountedPairAttackCommand.RecordHorsePrimaryAnimation: validation and metadata reads first, then the whole record.
            public void Record(FakeHandle handle, FakeAction action, string source)
            {
                if (strike.Recorded != null) throw new InvalidOperationException("duplicate record");
                if (strike.RecordFailsOnce) { strike.RecordFailsOnce = false; throw new InvalidOperationException("simulated metadata read failure"); }
                strike.Recorded = handle; strike.RecordedAction = action; strike.RecordedSource = source; strike.Records++;
            }
            public void Refresh(FakeHandle handle, FakeAction action)
            {
                if (strike.Recorded == null || ReferenceEquals(strike.Recorded, handle) || !ReferenceEquals(strike.RecordedAction, action))
                    throw new InvalidOperationException("inexact refresh");
                strike.Recorded = handle; strike.RecordedSource = HorseStrikeHandleBinding.StockCreated; strike.Refreshes++;
            }
            public bool MissingReported => strike.MissingReported;
            public void MarkMissingReported() => strike.MissingReported = true;
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

        // A hoof strike as the adapter classifies it: its hand is limb1 or limb2, so IsHorseBiteSlot is false.
        private static Strike HoofStrike(FakeHandle handle = null)
        {
            var strike = HorseStrike(handle);
            strike.Exact = false; strike.Weapon = "Hoof1d4"; strike.Special = "Hoof"; strike.Slot = "limb1";
            return strike;
        }

        // Review R4-F7: the command already recorded a handle (adopted or supplied); native then plans a strike of the same
        // command with no handle. The fake record throws on a second Record, as MountedPairAttackCommand does, so any fallback
        // attempt would surface here as a fault.
        private static void MissingAfterRecordedLeavesStrikeIntact()
        {
            var adopted = HorseStrike(new FakeHandle { Action = BiteAction });
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, adopted.Bind().Outcome, "The first stock handle was not adopted.");
            var recorded = adopted.Recorded;
            adopted.Handle = null;
            var missing = adopted.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.MissingAfterRecorded, missing.Outcome, "A missing handle after adoption was not reported distinctly.");
            TestRunner.True(adopted.Handle == null && adopted.Records == 1 && adopted.Creates == 0 && adopted.Assigns == 0 &&
                ReferenceEquals(adopted.Recorded, recorded) && ReferenceEquals(missing.Action, BiteAction),
                "A fallback was created, assigned or recorded after adoption.");
            var supplied = HorseStrike();
            TestRunner.Equal(HorseStrikeHandleOutcome.Supplied, supplied.Bind().Outcome, "The first fallback was not supplied.");
            supplied.Handle = null;
            TestRunner.Equal(HorseStrikeHandleOutcome.MissingAfterRecorded, supplied.Bind().Outcome, "A missing handle after a supplied one was not reported.");
            TestRunner.True(supplied.Records == 1 && supplied.Creates == 1 && supplied.Assigns == 1 && supplied.Handle == null,
                "A second fallback was created or assigned after a supplied handle.");
            // A later hoof strike of the same command, planned without a handle, after a recorded bite: not the Bite limb, so
            // not exact; untouched.
            var hoof = HorseStrike(new FakeHandle { Action = BiteAction });
            hoof.Bind();
            hoof.Exact = false; hoof.Weapon = "Hoof1d4"; hoof.Handle = null;
            TestRunner.Equal(HorseStrikeHandleOutcome.NotExact, hoof.Bind().Outcome, "A later hoof strike was treated as the Bite.");
            TestRunner.True(hoof.Records == 1 && hoof.Creates == 0 && hoof.Assigns == 0 && hoof.Handle == null && ReferenceEquals(hoof.RecordedAction, BiteAction),
                "The later hoof strike changed the record or the planned attack.");
        }

        private static void StockAdoptionOnce()
        {
            var stock = new FakeHandle { Action = BiteAction };
            var strike = HorseStrike(stock);
            var first = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, first.Outcome, "The stock Bite handle was not adopted.");
            TestRunner.True(ReferenceEquals(strike.Recorded, stock) && strike.RecordedSource == HorseStrikeHandleBinding.StockCreated &&
                ReferenceEquals(strike.Handle, stock) && strike.Assigns == 0 && strike.Creates == 0, "Adoption replaced or recreated the stock handle.");
            var again = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.AlreadyRecorded, again.Outcome, "The adopted handle was bound twice.");
            TestRunner.Equal(1, strike.Records, "The stock handle was recorded more than once.");
        }

        private static void RefreshSameActionOnly()
        {
            var secondBite = new FakeAction { Name = "HorseAnimationSet_Bite2", Type = "Bite" };
            var strike = HorseStrike(new FakeHandle { Action = BiteAction });
            strike.Actions.Add(secondBite);
            strike.Bind();
            var later = new FakeHandle { Action = BiteAction, Manager = strike.Manager };
            strike.Handle = later;
            var refreshed = strike.Bind();
            TestRunner.Equal(HorseStrikeHandleOutcome.Refreshed, refreshed.Outcome, "A later stock handle of the same action did not refresh the record.");
            TestRunner.True(ReferenceEquals(strike.Recorded, later) && strike.Records == 1 && strike.Refreshes == 1, "The refresh recorded a second handle.");
            strike.Handle = new FakeHandle { Action = secondBite, Manager = strike.Manager };
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedRefreshAction, strike.Bind().Outcome, "A refresh to a different Bite action was accepted.");
            strike.Handle = new FakeHandle { Action = HoofAction, Manager = strike.Manager };
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, strike.Bind().Outcome, "A later hoof-playing handle was accepted for the Bite.");
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
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedCandidateCount, none.Bind().Outcome, "Zero Bite actions were not refused.");
            var two = HorseStrike(); two.Actions.Add(new FakeAction { Name = "SecondBite", Type = "Bite" });
            var twoResult = two.Bind();
            TestRunner.True(twoResult.Outcome == HorseStrikeHandleOutcome.RejectedCandidateCount && twoResult.CandidateCount == 2, "Two Bite actions were not refused.");
            var failing = HorseStrike(); failing.CreateFails = true;
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedCreateFailed, failing.Bind().Outcome, "A failed creation was not refused.");
            var foreign = HorseStrike(new FakeHandle { Action = BiteAction }); foreign.Handle.Manager = new object();
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, foreign.Bind().Outcome, "A handle from another manager was adopted.");
            var stranger = HorseStrike(new FakeHandle { Action = new FakeAction { Name = "Unlisted", Type = "Bite" } });
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, stranger.Bind().Outcome, "An action outside the Horse set was adopted.");
            var dead = HorseStrike(new FakeHandle { Action = BiteAction }); dead.ManagerAlive = false;
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, dead.Bind().Outcome, "A handle was adopted without a live manager.");
            foreach (var strike in new[] { none, two, failing, foreign, stranger, dead })
                TestRunner.True(strike.Records == 0 && strike.Assigns == 0 && strike.Refreshes == 0, "A refused binding recorded or assigned a handle.");
        }

        // PR-5 review R1-F1. The adapter's context is exact only for the proven Bite limb (IsHorseBiteSlot, checked on genuine
        // slots and blueprints by the contract); the binding additionally requires the action itself to be a Bite, with no
        // wildcard. Before any Bite registration: a current hoof with a stock handle, a current hoof without one, an untyped
        // weapon whose only candidate is Hoof, and a Hoof handle presented in an exact context all leave zero Bite records,
        // creations and assignments. Positive controls: exact Bite adoption and exact Bite fallback.
        private static void HoofNeverBindsBite()
        {
            var hoofWithHandle = HoofStrike(new FakeHandle { Action = HoofAction });
            var hoofWithout = HoofStrike();
            var untypedOnlyHoof = HorseStrike(); untypedOnlyHoof.Special = "None"; untypedOnlyHoof.Actions.Clear(); untypedOnlyHoof.Actions.Add(HoofAction);
            var hoofHandleExact = HorseStrike(new FakeHandle { Action = HoofAction });
            TestRunner.Equal(HorseStrikeHandleOutcome.NotExact, hoofWithHandle.Bind().Outcome, "A hoof with a stock handle was bound as the Bite.");
            TestRunner.Equal(HorseStrikeHandleOutcome.NotExact, hoofWithout.Bind().Outcome, "A hoof without a handle reached the Bite fallback.");
            var untyped = untypedOnlyHoof.Bind();
            TestRunner.True(untyped.Outcome == HorseStrikeHandleOutcome.RejectedCandidateCount && untyped.CandidateCount == 0,
                "An untyped weapon with only a Hoof candidate received a Bite fallback.");
            TestRunner.Equal(HorseStrikeHandleOutcome.RejectedStockMismatch, hoofHandleExact.Bind().Outcome, "A Hoof action was adopted as the Bite.");
            foreach (var strike in new[] { hoofWithHandle, hoofWithout, untypedOnlyHoof, hoofHandleExact })
                TestRunner.True(strike.Records == 0 && strike.Assigns == 0 && strike.Creates == 0 && strike.Recorded == null,
                    "A hoof populated the Bite registration or was assigned a Bite fallback.");
            TestRunner.True(hoofWithout.Handle == null && ReferenceEquals(hoofWithHandle.Handle.Action, HoofAction), "A hoof's own planned handle was changed.");
            var biteAdopted = HorseStrike(new FakeHandle { Action = BiteAction });
            var biteSupplied = HorseStrike();
            TestRunner.True(biteAdopted.Bind().Outcome == HorseStrikeHandleOutcome.Adopted && biteSupplied.Bind().Outcome == HorseStrikeHandleOutcome.Supplied &&
                ReferenceEquals(biteSupplied.Handle.Action, BiteAction), "The exact Bite positive controls failed.");
        }

        // PR-5 review R1-F2: SupplyExact is SupplyContained(build, report, reportFault). A throwing outcome report (logger or
        // metadata) hands its exception to the fault report; a throwing fault report (the logger again) is absorbed; a throwing
        // boundary construction is contained too. Each call returns normally and the planned slot keeps what the binding did.
        private static void ReportingContained()
        {
            var adoption = HorseStrike(new FakeHandle { Action = BiteAction });
            var stock = adoption.Handle;
            Exception faulted = null;
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(() => new Boundary(adoption),
                result => { throw new InvalidOperationException("simulated outcome logger failure"); }, exception => faulted = exception);
            TestRunner.True(faulted is InvalidOperationException && faulted.Message.Contains("outcome logger") && ReferenceEquals(adoption.Handle, stock) &&
                adoption.Records == 1 && adoption.Assigns == 0, "A throwing outcome report escaped or changed the planned slot.");
            var both = HorseStrike();
            var returned = false;
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(() => new Boundary(both),
                result => { throw new InvalidOperationException("simulated outcome logger failure"); },
                exception => { throw new InvalidOperationException("simulated exception logger failure"); });
            returned = true;
            TestRunner.True(returned && both.Records == 1 && both.Handle != null && ReferenceEquals(both.Handle, both.Recorded),
                "A throwing fault report escaped, or the supplied handle was lost.");
            Exception buildFault = null;
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(() => { throw new NullReferenceException("simulated visual parameters read"); },
                result => { }, exception => buildFault = exception);
            TestRunner.True(buildFault is NullReferenceException, "A throwing boundary construction was not contained.");
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(null, null, null);
        }

        // PR-5 review R1-F3: the fallback assigns the slot and then records; a record (or metadata) fault restores the exact prior
        // slot (here: no handle) and leaves no registration. Inspected immediately and on retry, through the contained path.
        private static void RecordFaultLeavesNoPartialFallback()
        {
            var strike = HorseStrike(); strike.RecordFailsOnce = true;
            var faults = 0;
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(() => new Boundary(strike), result => { }, exception => faults++);
            TestRunner.True(faults == 1 && strike.Handle == null && strike.Recorded == null && strike.RecordedSource == null && strike.Records == 0 &&
                strike.NonNullAssigns == 1 && strike.Assigns == 2, "A record fault left an assigned fallback or a partial record.");
            HorseStrikeHandleBindingResult<FakeHandle, FakeAction> retry = default(HorseStrikeHandleBindingResult<FakeHandle, FakeAction>);
            HorseStrikeHandleBinding.SupplyContained<FakeHandle, FakeAction>(() => new Boundary(strike), result => retry = result, exception => faults++);
            TestRunner.True(faults == 1 && retry.Outcome == HorseStrikeHandleOutcome.Supplied && ReferenceEquals(strike.Handle, retry.Handle) &&
                ReferenceEquals(strike.Recorded, retry.Handle) && strike.RecordedSource == HorseStrikeHandleBinding.KmcSupplied && strike.Records == 1 &&
                strike.NonNullAssigns == 2 && strike.Creates == 2, "The retry did not supply exactly once with the kmc-supplied source.");
        }

        private static string Terminal(HorseBiteObservation<FakeHandle> observation, Strike command, int routine = 3) =>
            HorseBiteTerminal.Describe(observation, command.Recorded, command.RecordedSource, routine,
                handle => handle.Action?.Name, handle => handle.Speed, handle => handle.Acted, handle => handle.Finished, handle => handle.Interrupted);

        // PR-5 review R1-F4: the registration stays historical (idempotence), the observation follows the Bite at its strike.
        private static void TerminalDescribesObservedBite()
        {
            // Adoption, then native replans the Bite without a handle and the Bite strikes with none.
            var adopted = HorseStrike(new FakeHandle { Action = BiteAction, Acted = true, Finished = true, Speed = 2.4f });
            adopted.Bind();
            var observation = new HorseBiteObservation<FakeHandle>();
            observation.Observe(true, adopted.Handle);
            adopted.Handle = null;
            TestRunner.Equal(HorseStrikeHandleOutcome.MissingAfterRecorded, adopted.Bind().Outcome, "The replanned Bite was not left as native planned it.");
            observation.Observe(true, null);
            var replanned = Terminal(observation, adopted);
            TestRunner.True(replanned == "{observed=no-handle,routineAttacks=3}", "A handle-less replanned Bite was described with the superseded handle: " + replanned);
            // A valid Bite, then a handle-less hoof: the hoof is not a Bite observation.
            var valid = HorseStrike(new FakeHandle { Action = BiteAction, Started = true, Acted = true, Finished = true, Speed = 2.4f });
            valid.Bind();
            var second = new HorseBiteObservation<FakeHandle>();
            second.Observe(true, valid.Handle);
            second.Observe(false, null);
            var withHoof = Terminal(second, valid);
            TestRunner.True(withHoof == "{observed=handle,action=HorseAnimationSet_Bite,source=stock-created,speedScale=2.4,routineAttacks=3,acted=True,finished=True,interrupted=False}",
                "A valid Bite followed by a handle-less hoof was misdescribed: " + withHoof);
            // An exact Horse command with no observed Bite (ended before its strike), after a registration: not-reached.
            var notReached = Terminal(new HorseBiteObservation<FakeHandle>(), valid);
            TestRunner.True(notReached == "{observed=not-reached,routineAttacks=3}", "An unobserved Bite was described from the registration: " + notReached);
            // A Bite observed with a handle that was never registered (refused binding) states its own facts and source=unrecorded.
            var refused = HorseStrike(new FakeHandle { Action = BiteAction, Acted = true }); refused.Handle.Manager = new object();
            refused.Bind();
            var third = new HorseBiteObservation<FakeHandle>();
            third.Observe(true, refused.Handle);
            TestRunner.True(Terminal(third, refused).Contains("source=unrecorded") && Terminal(third, refused).Contains("acted=True"),
                "An unregistered observed Bite handle was not stated as unrecorded.");
            var failing = new HorseBiteObservation<FakeHandle>();
            failing.Observe(true, valid.Handle);
            var unreadable = HorseBiteTerminal.Describe(failing, valid.Recorded, valid.RecordedSource, 3, handle => { throw new InvalidOperationException("x"); },
                handle => 1f, handle => true, handle => true, handle => false);
            TestRunner.True(unreadable.Contains("facts=unreadable(InvalidOperationException)") && unreadable.Contains("source=stock-created"),
                "An unreadable observed handle was not stated as unreadable: " + unreadable);
        }

        // PR-5 review R1-F5: HorseStrikeDiagnostic.Slot resolves the planned hand by reference against the Horse's body.
        private static void StrikeSlotIdentity()
        {
            object primary = new object(), secondary = new object(), bite = new object(), hoofLeft = new object(), hoofRight = new object();
            var limbs = new List<object> { bite, hoofLeft, hoofRight };
            TestRunner.Equal("limb0", HorseStrikeDiagnostic.Slot(bite, primary, secondary, limbs), "The Bite limb was not identified.");
            TestRunner.True(HorseStrikeDiagnostic.Slot(hoofLeft, primary, secondary, limbs) == "limb1" && HorseStrikeDiagnostic.Slot(hoofRight, primary, secondary, limbs) == "limb2",
                "Two hooves sharing one blueprint were not distinguished by slot.");
            TestRunner.True(HorseStrikeDiagnostic.Slot(hoofLeft, primary, secondary, limbs) == HorseStrikeDiagnostic.Slot(hoofLeft, primary, secondary, limbs),
                "Repeated strikes from one limb resolved to different slots.");
            TestRunner.True(HorseStrikeDiagnostic.Slot(primary, primary, secondary, limbs) == "primary" && HorseStrikeDiagnostic.Slot(secondary, primary, secondary, limbs) == "secondary" &&
                HorseStrikeDiagnostic.Slot(new object(), primary, secondary, limbs) == "unresolved" && HorseStrikeDiagnostic.Slot(null, primary, secondary, limbs) == "none",
                "Hands, a foreign slot or a missing hand were misidentified.");
            var first = HorseStrike(new FakeHandle { Action = HoofAction, Started = true }); first.Slot = "limb1"; first.Special = "Hoof"; first.Weapon = "Hoof1d4";
            var repeat = Read(first, 3);
            var other = HorseStrike(new FakeHandle { Action = HoofAction, Started = true }); other.Slot = "limb2"; other.Special = "Hoof"; other.Weapon = "Hoof1d4";
            TestRunner.True(Read(first, 2).Format().Contains("index=2/3; slot=limb1") && repeat.Format().Contains("index=3/3; slot=limb1") &&
                Read(other, 3).Format().Contains("slot=limb2"), "The strike line did not keep the slot separate from the routine ordinal.");
        }

        // PR-5 review R1-F6: a throwing clip/weight getter or handle fact read marks only that group.
        // PR-5 review R2-F2: the contained observation step (the command's ObserveHorseStrike runs only this, then base.OnAction).
        // A failing plan read and a failing limb read after an earlier observed Bite: no exception escapes, exactly one strike line
        // names the Horse, and the observation becomes an explicit unreadable disposition (the stale Bite never stands for it).
        private static void ObservationStepContainsFailures()
        {
            foreach (var failAt in new[] { "plan", "limb", "handle" })
            {
                var observation = new HorseBiteObservation<FakeHandle>();
                var earlier = new FakeHandle { Action = BiteAction, Acted = true };
                HorseStrikeObservationStep.Observe<object, FakeHandle>(observation, () => new object(), plan => HorseBiteClass.Bite, plan => earlier,
                    handle => HorseStrikeHandleBinding.StockCreated, plan => "Horse strike: horse=horse-1; index=1/3; slot=limb0.", () => "horse-1", line => { });
                TestRunner.True(observation.Kind == HorseBiteObservationKind.Handle && ReferenceEquals(observation.Handle, earlier),
                    "The earlier Bite was not observed (" + failAt + ").");
                var lines = new List<string>();
                var delegated = 0;
                Exception escaped = null;
                try
                {
                    HorseStrikeObservationStep.Observe<object, FakeHandle>(observation,
                        () => { if (failAt == "plan") throw new InvalidOperationException("simulated planned-attack read"); return new object(); },
                        plan => { if (failAt == "limb") throw new NullReferenceException("simulated limb read"); return HorseBiteClass.Bite; },
                        plan => { if (failAt == "handle") throw new InvalidOperationException("simulated handle read"); return null; },
                        handle => HorseStrikeHandleBinding.StockCreated,
                        plan => "Horse strike: horse=horse-1; index=2/3; slot=limb0.", () => "horse-1", lines.Add);
                    delegated++;   // the command's base.OnAction follows the step (OnAction: ObserveHorseStrike, then base.OnAction)
                }
                catch (Exception exception) { escaped = exception; }
                var terminal = HorseBiteTerminal.Describe(observation, earlier, HorseStrikeHandleBinding.StockCreated, 3,
                    handle => handle.Action?.Name, handle => handle.Speed, handle => handle.Acted, handle => handle.Finished, handle => handle.Interrupted);
                TestRunner.True(escaped == null && delegated == 1 && lines.Count == 1 && lines[0].Contains("horse=horse-1") &&
                    lines[0].Contains("biteObservation=unreadable(" + failAt + ":") && observation.Kind == HorseBiteObservationKind.Unreadable &&
                    observation.Handle == null && terminal.StartsWith("{observed=unreadable(" + failAt + ":", StringComparison.Ordinal) && !terminal.Contains("observed=handle"),
                    "R2-F2 (" + failAt + "): the failure escaped, was not stated once for the Horse, or left the stale Bite: " + escaped + " | " +
                    string.Join(" || ", lines.ToArray()) + " | " + terminal);
            }
            // The strike line itself failing: the observation still records, and one fallback line names the Horse.
            var observed = new HorseBiteObservation<FakeHandle>(); var lineOut = new List<string>(); var handleNow = new FakeHandle { Action = BiteAction };
            HorseStrikeObservationStep.Observe<object, FakeHandle>(observed, () => new object(), plan => HorseBiteClass.Bite, plan => handleNow,
                handle => HorseStrikeHandleBinding.StockCreated, plan => { throw new InvalidOperationException("simulated reader fault"); }, () => "horse-1", lineOut.Add);
            TestRunner.True(observed.Kind == HorseBiteObservationKind.Handle && ReferenceEquals(observed.Handle, handleNow) && lineOut.Count == 1 &&
                lineOut[0].StartsWith("Horse strike: horse=horse-1", StringComparison.Ordinal), "A failing strike line suppressed the observation or its line.");
            // Control: a readable hoof after a Bite leaves the Bite observation (a hoof never changes it).
            var bitten = new HorseBiteObservation<FakeHandle>(); var biteHandle = new FakeHandle { Action = BiteAction };
            HorseStrikeObservationStep.Observe<object, FakeHandle>(bitten, () => new object(), plan => HorseBiteClass.Bite, plan => biteHandle, handle => HorseStrikeHandleBinding.StockCreated, plan => "l", () => "horse-1", line => { });
            HorseStrikeObservationStep.Observe<object, FakeHandle>(bitten, () => new object(), plan => HorseBiteClass.NotBite, plan => new FakeHandle { Action = HoofAction }, handle => HorseBiteTerminal.Unrecorded, plan => "l", () => "horse-1", line => { });
            TestRunner.True(bitten.Kind == HorseBiteObservationKind.Handle && ReferenceEquals(bitten.Handle, biteHandle) && bitten.BiteStrikes == 1,
                "A hoof strike changed the Bite observation.");
        }

        // PR-5 review R2-F4: a handle whose variant-count getter throws keeps its readable action and speed, and its separately
        // readable BiteAttack clip, weight and state, in the single strike line, naming the failed read.
        private static void HandleMetadataFaultKeepsPlayback()
        {
            var strike = HorseStrike(new FakeHandle { Action = BiteAction, Started = true, Acted = true, Speed = 2.4f, Variant = 1, VariantsThrow = true,
                Clip = "BiteAttack", Weight = 0.8f, State = "Playing" });
            strike.Bind();
            var line = Read(strike).Format();
            TestRunner.True(line.Contains("handle=started,acted,facts-unreadable(variants:InvalidOperationException)") &&
                line.Contains("action=HorseAnimationSet_Bite") && line.Contains("speedScale=2.4") && line.Contains("variant=1/unreadable") &&
                line.Contains("activeClip=BiteAttack") && line.Contains("clipWeight=0.8") && line.Contains("animationState=Playing") &&
                line.Split(new[] { "Horse strike:" }, StringSplitOptions.None).Length == 2,
                "R2-F4: a handle metadata fault suppressed readable facts or playback: " + line);
        }

        private sealed class TerminalOutcome { public string Detail; public string Bite; public int Attached; }

        // PR-5 review R2-F3: the production composer: an unrelated detailed-outcome read fails after a known Bite observation;
        // the one terminal callback receives the minimal outcome carrying that disposition. A non-Horse command attaches null.
        private static void MinimalTerminalKeepsBite()
        {
            var observation = new HorseBiteObservation<FakeHandle>();
            var handle = new FakeHandle { Action = BiteAction, Acted = true, Finished = true };
            observation.Observe(true, handle);
            var callbacks = new List<TerminalOutcome>();
            Func<HorseBiteObservation<FakeHandle>, string> describe = obs => HorseBiteTerminal.Describe(obs, handle, HorseStrikeHandleBinding.StockCreated, 3,
                h => h.Action?.Name, h => h.Speed, h => h.Acted, h => h.Finished, h => h.Interrupted);
            Exception logged = null;
            callbacks.Add(HorseTerminalOutcome.Compose<TerminalOutcome>(
                () => { throw new NullReferenceException("simulated unrelated detailed read (target despawned)"); },
                exception => { logged = exception; return new TerminalOutcome { Detail = "minimal" }; },
                outcome => { outcome.Attached++; outcome.Bite = describe(observation); }));
            TestRunner.True(callbacks.Count == 1 && callbacks[0].Detail == "minimal" && callbacks[0].Attached == 1 && logged is NullReferenceException &&
                callbacks[0].Bite != null && callbacks[0].Bite.StartsWith("{observed=handle,action=HorseAnimationSet_Bite,source=stock-created", StringComparison.Ordinal),
                "R2-F3: the minimal terminal outcome lost the Bite disposition: " + callbacks[0].Bite);
            var detailed = HorseTerminalOutcome.Compose<TerminalOutcome>(() => new TerminalOutcome { Detail = "detailed" }, exception => null,
                outcome => { outcome.Attached++; outcome.Bite = describe(observation); });
            var nonHorse = HorseTerminalOutcome.Compose<TerminalOutcome>(() => { throw new InvalidOperationException("x"); }, exception => new TerminalOutcome { Detail = "minimal" },
                outcome => { outcome.Attached++; outcome.Bite = null; });
            var failingAttach = HorseTerminalOutcome.Compose<TerminalOutcome>(() => new TerminalOutcome { Detail = "detailed" }, exception => null,
                outcome => { throw new InvalidOperationException("simulated attach fault"); });
            TestRunner.True(detailed.Detail == "detailed" && detailed.Attached == 1 && detailed.Bite == callbacks[0].Bite && nonHorse.Bite == null && nonHorse.Attached == 1 &&
                failingAttach.Detail == "detailed", "R2-F3: the detailed path, the non-Horse control or a failing attachment misbehaved.");
        }

        private static HorseBitePlayback Playback(HorseBiteObservation<FakeHandle> observation, Strike command) =>
            HorseBiteTerminal.Project(observation, command.Recorded, command.RecordedSource, h => h.Action?.Name, h => h.Action?.Type,
                h => h.Acted, h => h.Finished, h => h.Interrupted);

        // PR-5 review R1-F4: the structured playback projection agrees with the observed strike. After stock adoption, a Bite that
        // strikes without a handle: the formatted line, the projection and the scenario playback check all say no handle, while
        // the registration stays visible as historical. Control: a positive observed Bite followed by a hoof is accepted.
        private static void StructuredPlaybackFollowsObservation()
        {
            var stock = new FakeHandle { Action = BiteAction, Started = true, Acted = true, Finished = true };
            var command = HorseStrike(stock);
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, command.Bind().Outcome, "The stock handle was not adopted.");
            var observation = new HorseBiteObservation<FakeHandle>();
            observation.Observe(true, null);
            var negative = Playback(observation, command);
            var line = Terminal(observation, command);
            TestRunner.True(line.StartsWith("{observed=no-handle", StringComparison.Ordinal) && negative.Observation == "no-handle" && !negative.HandleObserved &&
                negative.Source == null && !negative.Acted && negative.RegisteredHandle && negative.RegisteredSource == HorseStrikeHandleBinding.StockCreated &&
                !HorseBiteTerminal.PlaybackAccepted(negative) && !HorseBiteTerminal.PlaybackAccepted(negative.HandleObserved, negative.Source, negative.Interrupted, negative.FactsUnreadable),
                "R1-F4: a handle-less Bite after adoption produced positive structured playback evidence.");
            // A different handle observed than the registered one: its own facts, source unrecorded, rejected.
            var other = new FakeHandle { Action = BiteAction, Acted = true };
            var differs = new HorseBiteObservation<FakeHandle>(); differs.Observe(true, other);
            var differing = Playback(differs, command);
            TestRunner.True(differing.HandleObserved && differing.Source == HorseBiteTerminal.Unrecorded && !HorseBiteTerminal.PlaybackAccepted(differing),
                "R1-F4: an unregistered observed handle was accepted as exact playback.");
            // Control: the registered Bite observed at its strike, then a hoof.
            var positive = new HorseBiteObservation<FakeHandle>(); positive.Observe(true, stock); positive.Observe(false, new FakeHandle { Action = HoofAction });
            var accepted = Playback(positive, command);
            TestRunner.True(accepted.Observation == "handle" && accepted.HandleObserved && accepted.Source == HorseStrikeHandleBinding.StockCreated &&
                accepted.ActionName == "HorseAnimationSet_Bite" && accepted.Acted && accepted.Finished && HorseBiteTerminal.PlaybackAccepted(accepted) &&
                Terminal(positive, command).StartsWith("{observed=handle,action=HorseAnimationSet_Bite,source=stock-created", StringComparison.Ordinal),
                "R1-F4 control: an observed registered Bite followed by a hoof was not accepted.");
            // Unreadable and not-reached project as rejected, with the reason stated.
            var unreadable = new HorseBiteObservation<FakeHandle>(); unreadable.ObserveUnreadable("plan:InvalidOperationException");
            TestRunner.True(Playback(unreadable, command).Observation == "unreadable(plan:InvalidOperationException)" && !HorseBiteTerminal.PlaybackAccepted(Playback(unreadable, command)) &&
                Playback(new HorseBiteObservation<FakeHandle>(), command).Observation == "not-reached", "R1-F4: unreadable or unreached Bites were not stated.");
        }

        // PR-5 review R2-F2 (round 4): a missing plan, or a plan without the inputs that prove Bite or not-Bite, after a known Bite:
        // one identifiable line and an explicit unavailable observation; a positively identified hoof keeps the earlier Bite.
        private static void MissingInputsAreUnavailable()
        {
            foreach (var kind in new[] { "plan:missing", "limb:unavailable" })
            {
                var observation = new HorseBiteObservation<FakeHandle>(); var earlier = new FakeHandle { Action = BiteAction };
                observation.Observe(true, earlier, HorseStrikeHandleBinding.StockCreated);
                var lines = new List<string>();
                HorseStrikeObservationStep.Observe<object, FakeHandle>(observation, () => kind == "plan:missing" ? null : new object(),
                    plan => HorseBiteClass.Unavailable, plan => new FakeHandle(), handle => HorseBiteTerminal.Unrecorded, plan => "Horse strike: horse=horse-1; index=2/3.",
                    () => "horse-1", lines.Add);
                TestRunner.True(observation.Kind == HorseBiteObservationKind.Unreadable && observation.UnreadableReason == kind && observation.Handle == null &&
                    lines.Count == 1 && lines[0].Contains("horse=horse-1") && lines[0].Contains("biteObservation=unreadable(" + kind + ")"),
                    "R2-F2: missing inputs (" + kind + ") left the earlier Bite or were not stated once: " + string.Join(" || ", lines.ToArray()));
            }
            var bitten = new HorseBiteObservation<FakeHandle>(); var bite = new FakeHandle { Action = BiteAction };
            bitten.Observe(true, bite, HorseStrikeHandleBinding.StockCreated);
            HorseStrikeObservationStep.Observe<object, FakeHandle>(bitten, () => new object(), plan => HorseBiteClass.NotBite, plan => new FakeHandle { Action = HoofAction },
                handle => HorseBiteTerminal.Unrecorded, plan => "l", () => "horse-1", line => { });
            TestRunner.True(bitten.Kind == HorseBiteObservationKind.Handle && ReferenceEquals(bitten.Handle, bite), "R2-F2: a positively identified hoof changed the Bite.");
        }

        // PR-5 review R3-F4: the source is captured with the observed Bite. A registered Bite is observed, the registration then
        // refreshes to another handle of the same action, and the command ends before another Bite acts: both terminal forms keep
        // the original source. Control: a Bite observed with an unregistered handle stays unrecorded.
        private static void ObservedSourceSurvivesRefresh()
        {
            var first = new FakeHandle { Action = BiteAction, Acted = true, Finished = true };
            var command = HorseStrike(first);
            TestRunner.Equal(HorseStrikeHandleOutcome.Adopted, command.Bind().Outcome, "The first Bite handle was not adopted.");
            var observation = new HorseBiteObservation<FakeHandle>();
            HorseStrikeObservationStep.Observe<object, FakeHandle>(observation, () => new object(), plan => HorseBiteClass.Bite, plan => first,
                handle => ReferenceEquals(handle, command.Recorded) ? command.RecordedSource : HorseBiteTerminal.Unrecorded, plan => "l", () => "horse-1", line => { });
            var later = new FakeHandle { Action = BiteAction, Manager = command.Manager };
            command.Handle = later;
            TestRunner.Equal(HorseStrikeHandleOutcome.Refreshed, command.Bind().Outcome, "The later same-action handle did not refresh the registration.");
            var text = Terminal(observation, command);
            var playback = Playback(observation, command);
            TestRunner.True(!ReferenceEquals(command.Recorded, first) && text.Contains("source=stock-created") && playback.Source == HorseStrikeHandleBinding.StockCreated &&
                HorseBiteTerminal.PlaybackAccepted(playback), "R3-F4: a refresh rewrote the source of the observed Bite: " + text);
            var unregistered = new HorseBiteObservation<FakeHandle>(); var stray = new FakeHandle { Action = BiteAction };
            HorseStrikeObservationStep.Observe<object, FakeHandle>(unregistered, () => new object(), plan => HorseBiteClass.Bite, plan => stray,
                handle => ReferenceEquals(handle, command.Recorded) ? command.RecordedSource : HorseBiteTerminal.Unrecorded, plan => "l", () => "horse-1", line => { });
            TestRunner.True(Terminal(unregistered, command).Contains("source=unrecorded") && Playback(unregistered, command).Source == HorseBiteTerminal.Unrecorded &&
                !HorseBiteTerminal.PlaybackAccepted(Playback(unregistered, command)), "R3-F4 control: an unregistered observed handle gained a source.");
        }

        // PR-5 review R3-F3: a terminal fact-read failure on the registered observed handle: the projection carries it and the one
        // complete decision (also the scenario checks' four-argument form) rejects it; the readable positive passes.
        private static void UnreadableFactsRejected()
        {
            var handle = new FakeHandle { Action = BiteAction, Acted = true, Finished = true };
            var command = HorseStrike(handle); command.Bind();
            var observation = new HorseBiteObservation<FakeHandle>(); observation.Observe(true, handle, HorseStrikeHandleBinding.StockCreated);
            var failing = HorseBiteTerminal.Project(observation, command.Recorded, command.RecordedSource, h => h.Action?.Name, h => h.Action?.Type,
                h => { throw new InvalidOperationException("simulated terminal acted read"); }, h => h.Finished, h => h.Interrupted);
            var readable = Playback(observation, command);
            TestRunner.True(failing.HandleObserved && failing.FactsUnreadable == "InvalidOperationException" && !HorseBiteTerminal.PlaybackAccepted(failing) &&
                !HorseBiteTerminal.PlaybackAccepted(failing.HandleObserved, failing.Source, failing.Interrupted, failing.FactsUnreadable) &&
                HorseBiteTerminal.PlaybackAccepted(readable) && HorseBiteTerminal.PlaybackAccepted(readable.HandleObserved, readable.Source, readable.Interrupted, readable.FactsUnreadable),
                "R3-F3: an unreadable observed fact was accepted, or the readable positive rejected.");
        }

        private static void UnreadableObservationKeepsRecord()
        {
            var strike = HorseStrike(new FakeHandle { Action = BiteAction, Started = true, Acted = true, Speed = 2.4f, ClipThrows = true });
            strike.Bind();
            var line = Read(strike).Format();
            TestRunner.True(line.StartsWith("Horse strike: horse=horse-1; index=1/3; slot=limb0; weapon=Bite1d4; special=Bite; handle=started,acted; action=HorseAnimationSet_Bite; speedScale=2.4") &&
                line.Contains("playback=unreadable(InvalidOperationException)") && !line.Contains("activeClip=<none>"),
                "An unreadable playback read erased the strike or read as an absent clip: " + line);
            var facts = HorseStrike(new FakeHandle { Action = BiteAction, FactsThrow = true });
            var factsLine = Read(facts).Format();
            TestRunner.True(factsLine.Contains("weapon=Bite1d4") && factsLine.Contains("facts-unreadable(action:InvalidOperationException)") && factsLine.Contains("action=unreadable"),
                "An unreadable handle fact erased the strike or read as handle=none: " + factsLine);
            var absent = Read(HorseStrike()).Format();
            TestRunner.True(absent.Contains("handle=none") && !absent.Contains("unreadable"), "A positively absent handle was reported as unreadable.");
        }

        // PR-5 review R1-F7: the report-once flag is the command's (the fake Strike plays the command): repeated callbacks of one
        // command report once; a later command reports on its own. The adapter keeps no command reference (contract check).
        private static void MissingReportOncePerCommand()
        {
            var command = HorseStrike(new FakeHandle { Action = BiteAction });
            command.Bind(); command.Handle = null;
            var first = command.Bind(); var second = command.Bind(); var third = command.Bind();
            TestRunner.True(first.FirstMissingReport && !second.FirstMissingReport && !third.FirstMissingReport && command.MissingReported,
                "Repeated missing-handle callbacks of one command were not reported exactly once.");
            var later = HorseStrike(); later.Bind(); later.Handle = null;
            TestRunner.True(later.Bind().FirstMissingReport, "A subsequent command did not report independently.");
        }

        private static void InvariantCultureFormatting()
        {
            var saved = Thread.CurrentThread.CurrentCulture;
            try
            {
                Thread.CurrentThread.CurrentCulture = new CultureInfo("de-DE");
                var terminal = HorseStrikeDiagnostic.FormatTerminalBite(HorseBiteObservationKind.Handle, "HorseAnimationSet_Bite", "stock-created", 2.4f, 1234, true, false, false);
                var strike = HorseStrike(new FakeHandle { Action = BiteAction, Speed = 2.4f, Weight = 0.5f, Clip = "BiteAttack" });
                var line = Read(strike).Format();
                TestRunner.True(terminal.Contains("speedScale=2.4,") && terminal.Contains("routineAttacks=1234,") && !terminal.Contains("2,4") &&
                    line.Contains("speedScale=2.4;") && line.Contains("clipWeight=0.5;"), "A decimal-comma culture changed the diagnostic numbers: " + terminal + " / " + line);
            }
            finally { Thread.CurrentThread.CurrentCulture = saved; }
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
            public string SpecialType => strike.Special;
            public string Slot => strike.Slot;
            public FakeHandle Handle => strike.Handle;
            public string ActionName(FakeHandle handle) => handle.FactsThrow ? throw new InvalidOperationException("simulated handle read") : handle.Action?.Name;
            public bool IsStarted(FakeHandle handle) => handle.Started;
            public bool IsActed(FakeHandle handle) => handle.Acted;
            public bool IsInterrupted(FakeHandle handle) => handle.Interrupted;
            public float SpeedScale(FakeHandle handle) => handle.Speed;
            public int Variant(FakeHandle handle) => handle.Variant;
            public int VariantsCount(FakeHandle handle) => handle.VariantsThrow ? throw new InvalidOperationException("simulated variant-count read") : handle.Variants;
            public string ActiveClip(FakeHandle handle) => handle.ClipThrows ? throw new InvalidOperationException("simulated invalidated clip") : handle.Clip;
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
            // A hoof is described by the reader (never bound as the Bite).
            var hoof = HoofStrike(new FakeHandle { Action = HoofAction, Started = true, Acted = true, Clip = "Left_Hoof_Attack", Weight = 1f, State = "Playing" });
            TestRunner.Equal(HorseStrikeHandleOutcome.NotExact, hoof.Bind().Outcome, "The hoof was bound as the Bite.");
            var hoofRecord = Read(hoof, 2);
            TestRunner.True(hoofRecord.Format().Contains("special=Hoof") && hoofRecord.Format().Contains("activeClip=Left_Hoof_Attack") &&
                hoofRecord.Format().Contains("index=2/3") && hoofRecord.Format().Contains("slot=limb1") && hoofRecord.Action == "HorseAnimationSet_Hoof",
                "The hoof control was not distinguishable.");
        }
    }
}
