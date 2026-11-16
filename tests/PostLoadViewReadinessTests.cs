using System;
using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // Known issues S02 (2026-11-15): a pair restored at area arrival was attached to views that native had not yet
    // activated, and the first invariant check after the loader went idle dismounted it (CompanionInvalidated).
    //
    // What runs here is production code: the arrival and in-area decisions (MountedRetainedIntentPolicy), the view
    // readiness and invariant policies (MountedRestoreViewReadinessPolicy, MountedViewActivityPolicy), the transfer
    // lifecycle (MountedAreaTransferLifecycle), the real relationship coordinator and the real pair registry.
    // The harness below only stands where the integration stands: it holds the units, their views and the retained
    // rows, and calls those production pieces in the order MountedPersistenceService.RestoreAreaPair,
    // RestoreRetainedIntentCore and GameMountedRelationshipService.ValidateActivePair call them. That the compiled
    // integration makes the same calls in the same order is checked by scripts/Test-PostLoadViewReadinessContracts.ps1.
    // Nothing here is native evidence.
    internal static class PostLoadViewReadinessTests
    {
        public static void Register(TestRunner runner)
        {
            runner.Run("S02 restore readiness is exactly what the mounted invariant accepts in ordinary play, for all thirty-two observations", ReadinessIsTheSettledInvariant);
            runner.Run("S02 the prior arrival decision reproduces the recorded dismount; the readiness term attaches nothing to inactive views", PriorDecisionReproducesRecordedDismount);
            runner.Run("S02 1 recorded ordering: the pair attaches once when its exact views become active and stays valid", RecordedOrdering);
            runner.Run("S02 2 repeated arrival and readiness notifications never attach a pair twice", Idempotence);
            runner.Run("S02 3 an explicit dismount, combat, a replaced world and a new area transfer each stop the delayed restoration", Cancellation);
            runner.Run("S02 a pair deferred at arrival keeps the arrival placement's geometry only while neither unit has moved", ArrivalPlacementGeometry);
            runner.Run("S02 4 replaced views, removed actors, another world and an identity already in use cannot revive a pair", StaleIdentity);
            runner.Run("S02 5 inactive views in settled ordinary play still invalidate a mounted pair and are not restored", GenuineInvalidationUnchanged);
            runner.Run("S02 6 views that never become active end in a bounded clean release with nothing latched", NeverReadyIsBounded);
            runner.Run("S02 7 a restoration that leaves owned state keeps the protective latch", FailedCleanupKeepsLatch);
            runner.Run("S02 8 a waiting pair changes nothing for other pairs or unmounted actors, and nothing is mounted by a voluntary admission", Isolation);
            runner.Run("S02 a pair mounted at departure is deferred only when eligible with inactive views; every refusal keeps its outcome", DepartingArrival);
            runner.Run("S02 the native capital retirement and every other refusal are decided before view readiness", RefusalsPrecedeReadiness);
            runner.Run("S02 the settle and patience bounds and the explicit-dismount release are exact", BoundsAndRelease);
        }

        private sealed class View { internal bool Self = true, Hierarchy = true; internal bool Active => Self && Hierarchy; }

        private sealed class Actor
        {
            internal readonly string Id = Guid.NewGuid().ToString();
            internal View View = new View();
            internal bool Present = true, Controllable = true, Alive = true;
            // Position along one axis, in metres.
            internal float X;
            // Native "sleeping": UnitEntityData.IsViewActive is false, so the view object is inactive.
            internal void Sleep() { View.Self = false; View.Hierarchy = false; }
            internal void Wake() { View.Self = true; View.Hierarchy = true; }
        }

        private sealed class CountingRuntime : IMountedPairRuntime
        {
            internal int Authority, Attach, Restore;
            internal bool FailAttach, FailRollbackPresentation;
            public void AcquireMovementAuthority(MountedPair pair) { Authority++; }
            public void AttachPresentation(MountedPair pair) { Attach++; if (FailAttach) throw new InvalidOperationException("attach failed"); }
            public void RestorePresentation(MountedPair pair) { Restore++; if (FailRollbackPresentation) throw new InvalidOperationException("rollback failed"); }
            public void RestoreMovementAuthority(MountedPair pair, CleanupTrigger trigger) { }
        }

        // One pair boundary: what GameMountedRelationshipService holds for a pair.
        private sealed class Pair
        {
            internal readonly Actor Rider = new Actor(), Mount = new Actor();
            internal readonly CountingRuntime Runtime = new CountingRuntime();
            internal readonly MountedRelationshipCoordinator Coordinator;
            internal View CapturedRider, CapturedMount;
            internal MountedPairOwner<Actor, object> Owner;
            internal CleanupTrigger? LastTrigger;
            internal int VoluntaryMounts, SavedRestores;
            internal Pair() { Coordinator = new MountedRelationshipCoordinator(Runtime); }
            internal bool Mounted => Coordinator.State == RelationshipState.Mounted;
        }

        private sealed class Row
        {
            internal Pair Pair; internal Guid OwnerId; internal long Generation; internal float EligibleSince = -1f;
            internal object World;
            // AreaPair.ArrivalDeferred and the arrival placement of its rider and mount (transient).
            internal bool ArrivalDeferred; internal float ArrivalRiderX, ArrivalMountX;
        }

        private sealed class Harness
        {
            internal object World = new object();
            internal const string Campaign = "campaign";
            internal readonly MountedPairRegistry<Actor, object> Registry = new MountedPairRegistry<Actor, object>(a => a.Id);
            internal readonly List<Pair> Pairs = new List<Pair>();
            internal readonly List<Row> Retained = new List<Row>();
            internal readonly List<string> Dropped = new List<string>();
            internal string Latch;
            internal string Mode = "Default";
            internal bool Loading, BoundaryOwned, Combat, PartySelector;
            internal MountedRetainedIntentKind Kind = MountedRetainedIntentKind.WorldMapTravel;
            // False calls the arrival and in-area decisions exactly as the prior implementation called them.
            internal bool ConsultViews = true;
            private int settledFrames; private float settledSince = -1f;

            internal Pair Add() { var pair = new Pair(); Pairs.Add(pair); return pair; }

            private MountedPairCandidate Candidate(Pair pair, string mode) => new MountedPairCandidate(pair.Rider.Id, pair.Mount.Id)
            {
                PartyIsInCombat = Combat,
                RiderIsDirectlyControllable = pair.Rider.Controllable, MountIsDirectlyControllable = pair.Mount.Controllable,
                RiderIsAliveAndConscious = pair.Rider.Alive, MountIsAliveAndConscious = pair.Mount.Alive,
                ExactReciprocalCompanionRelationship = true, RiderSizeOrdinal = 4, MountSizeOrdinal = 5,
                // The native candidate reads the view and agent components, which exist and stay enabled on an inactive view.
                RiderViewAndStockAgentAvailable = true, MountViewAndStockAgentAvailable = true, RiderStockAgentEnabled = true,
                MountStockAgentEnabled = true, RiderAgentOverrideAvailable = true, MountAgentOverrideAvailable = true,
                // Production: MountedGameModePolicy.CanAdmitMountedAction, Default only.
                RiderIsExactlyMedium = true, SafeMovementMode = MountedGameModePolicy.CanAdmitMountedAction(mode)
            };

            private string ViewRefusal(Pair pair) => MountedRestoreViewReadinessPolicy.Refusal(
                pair.Rider.View.Self, pair.Mount.View.Self, pair.Rider.View.Hierarchy, pair.Mount.View.Hierarchy, PartySelector);

            // A voluntary Mount by the player: the exploration transition, or the voluntary combat control in a fight.
            internal void MountByPlayer(Pair pair, long generation = 1)
            {
                Registry.RetireSettled();
                TestRunner.True(Registry.TryReserve(World, Campaign, pair.Rider, pair.Mount, generation, pair, () => false, () => true, () => true,
                    out var owner, out var refusal), refusal);
                var result = pair.Coordinator.Mount(Candidate(pair, Mode),
                    Combat ? MountedRelationshipAdmission.VoluntaryCombat : MountedRelationshipAdmission.Exploration);
                TestRunner.True(result.Succeeded, "The voluntary mount failed: " + string.Join("; ", result.Errors));
                Registry.Commit(owner);
                pair.Owner = owner; pair.VoluntaryMounts++;
                pair.CapturedRider = pair.Rider.View; pair.CapturedMount = pair.Mount.View;
            }

            private void Detach(Pair pair, CleanupTrigger trigger)
            {
                var result = pair.Coordinator.Dismount(trigger);
                pair.LastTrigger = result.Trigger;
                if (pair.Owner != null && Registry.Owns(pair.Owner)) Registry.Detach(pair.Owner);
                pair.Owner = null; pair.CapturedRider = null; pair.CapturedMount = null;
            }

            // The area suspension that precedes world-map travel: the exact owner identity is captured, the pair is dismounted.
            internal Row Suspend(Pair pair)
            {
                var row = new Row { Pair = pair, OwnerId = pair.Owner.Id, Generation = pair.Owner.Generation, World = World };
                Detach(pair, CleanupTrigger.AreaSuspension);
                Retained.Add(row);
                return row;
            }

            // An explicit player dismount: the world's pair-dismount notification, then the dismount.
            internal void DismountByPlayer(Pair pair)
            {
                if (MountedRetainedIntentPolicy.ReleasedByDismount(CleanupTrigger.Manual))
                    Retained.RemoveAll(row => MountedRetainedIntentPolicy.SharesActor(row.Pair.Rider.Id, row.Pair.Mount.Id, pair.Rider.Id, pair.Mount.Id));
                Detach(pair, CleanupTrigger.Manual);
            }

            // RestoreRetainedPair: the captured identity is imported, the real coordinator re-attaches as a saved restore.
            private MountedRetainedRestoreOutcome Restore(Row row)
            {
                var pair = row.Pair;
                Registry.RetireSettled();
                if (!Registry.TryImport(World, Campaign, row.OwnerId, row.Generation, pair.Rider, pair.Mount, pair, true,
                        () => false, () => true, () => true, out var owner, out var refusal))
                    return MountedRetainedRestoreOutcome.CleanRefusal;
                var result = pair.Coordinator.RestoreSaved(Candidate(pair, Mode));
                pair.SavedRestores++;
                if (result.Succeeded)
                {
                    Registry.Commit(owner);
                    pair.Owner = owner; pair.CapturedRider = pair.Rider.View; pair.CapturedMount = pair.Mount.View;
                    return MountedRetainedRestoreOutcome.Restored;
                }
                Registry.Detach(owner);
                return MountedRetainedIntentPolicy.ClassifyRestore(false, result.MovementAuthorityResidual, result.PresentationResidual,
                    pair.Coordinator.State == RelationshipState.Unmounted, true, pair.Coordinator.State != RelationshipState.Faulted);
            }

            // RestoreAreaPair for carried pairs, at the native placement callback of one transfer.
            internal void Arrive(MountedAreaTransferLifecycle transfer)
            {
                if (!transfer.BeginRestore()) return;
                foreach (var row in Retained.ToArray())
                {
                    var pair = row.Pair;
                    var present = pair.Rider.Present && pair.Mount.Present;
                    var candidate = present ? Candidate(pair, Mode) : null;
                    var refusal = candidate?.ValidateSavedRelationship();
                    var permanent = candidate != null && (!candidate.ExactReciprocalCompanionRelationship ||
                        !candidate.RiderIsAliveAndConscious || !candidate.MountIsAliveAndConscious);
                    // A newer transfer copies its carried rows, so nothing of an earlier arrival placement travels.
                    row.ArrivalDeferred = false;
                    var viewRefusal = ConsultViews && present && refusal == null ? ViewRefusal(pair) : null;
                    var arrival = ConsultViews
                        ? MountedRetainedIntentPolicy.DecideCarriedArrival(Combat, present, present, permanent, refusal, false, viewRefusal)
                        : MountedRetainedIntentPolicy.DecideCarriedArrival(Combat, present, present, permanent, refusal, false);
                    if (arrival == MountedCarriedArrivalDecision.Retain)
                    {
                        // MarkArrivalDeferred: only a pair that nothing but its views held back.
                        if (viewRefusal != null) { row.ArrivalDeferred = true; row.ArrivalRiderX = pair.Rider.X; row.ArrivalMountX = pair.Mount.X; }
                        continue;
                    }
                    Retained.Remove(row);
                    if (arrival != MountedCarriedArrivalDecision.Restore) { Dropped.Add("arrival:" + arrival); continue; }
                    var outcome = Restore(row);
                    if (outcome == MountedRetainedRestoreOutcome.CleanRefusal) Retained.Add(row);
                    else if (outcome == MountedRetainedRestoreOutcome.RetainedResidue) { Latch = "arrival residue"; return; }
                }
            }

            // GameMountedRelationshipService.ValidateActivePair for every pair (MountedWorldService.UpdatePairs).
            internal void Validate()
            {
                // RefreshOwnership: settled detached owners retire before anything is judged.
                Registry.RetireSettled();
                foreach (var pair in Pairs)
                {
                    if (!pair.Mounted) continue;
                    if (!ReferenceEquals(pair.Rider.View, pair.CapturedRider) || !ReferenceEquals(pair.Mount.View, pair.CapturedMount))
                    { Detach(pair, CleanupTrigger.ViewReplaced); continue; }
                    if (!MountedViewActivityPolicy.IsAdmissible(MountedGameModePolicy.Classify(Mode), pair.Rider.View.Self, pair.Mount.View.Self,
                            pair.Rider.View.Hierarchy, pair.Mount.View.Hierarchy, PartySelector))
                        Detach(pair, CleanupTrigger.CompanionInvalidated);
                }
            }

            // RestoreRetainedIntentCore: one evaluation of the retained custody at the given unscaled time.
            internal void Evaluate(float now)
            {
                if (Retained.Count == 0) return;
                var sameWorld = Retained.All(row => ReferenceEquals(row.World, World));
                var decision = MountedRetainedIntentPolicy.Decide(Kind, sameWorld, Combat, false, Mode, false, Loading, BoundaryOwned, false);
                if (decision == MountedRetainedIntentDecision.Cancel) { Dropped.AddRange(Retained.Select(row => "cancelled")); Retained.Clear(); ResetSettle(); return; }
                if (decision == MountedRetainedIntentDecision.Hold) { ResetSettle(); return; }
                if (settledSince < 0f) settledSince = now;
                if (!MountedRetainedIntentPolicy.Settled(++settledFrames, now - settledSince)) return;
                foreach (var row in Retained.ToArray())
                {
                    var pair = row.Pair;
                    var present = pair.Rider.Present && pair.Mount.Present;
                    var candidate = present ? Candidate(pair, Mode) : null;
                    var refusal = candidate?.ValidateSavedRelationship();
                    var viewRefusal = ConsultViews && candidate != null && refusal == null ? ViewRefusal(pair) : null;
                    if (row.EligibleSince < 0f) row.EligibleSince = now;
                    var expired = MountedRetainedIntentPolicy.PatienceExpired(row.EligibleSince, now);
                    var permanent = candidate != null && (!candidate.ExactReciprocalCompanionRelationship ||
                        !candidate.RiderIsAliveAndConscious || !candidate.MountIsAliveAndConscious);
                    var alreadyMounted = Pairs.Any(other => other.Coordinator.State != RelationshipState.Unmounted &&
                        (ReferenceEquals(other.Rider, pair.Rider) || ReferenceEquals(other.Mount, pair.Mount)));
                    var held = MountedRetainedIntentPolicy.ArrivalPlacementHeld(row.ArrivalDeferred && present,
                        Math.Abs(pair.Rider.X - row.ArrivalRiderX), Math.Abs(pair.Mount.X - row.ArrivalMountX));
                    var pairDecision = MountedRetainedIntentPolicy.DecidePair(candidate != null, candidate != null, alreadyMounted, permanent,
                        refusal, Math.Abs(pair.Rider.X - pair.Mount.X), MountedRetainedIntentPolicy.MaximumSeparationMetres(Kind), true, expired,
                        false, MountedRegroupState.Unavailable, false, viewRefusal, held);
                    if (pairDecision == MountedRetainedPairDecision.Retry) continue;
                    if (pairDecision == MountedRetainedPairDecision.Drop)
                    {
                        var separated = candidate != null && Math.Abs(pair.Rider.X - pair.Mount.X) > MountedRetainedIntentPolicy.MaximumSeparationMetres(Kind);
                        Retained.Remove(row); Dropped.Add(refusal ?? (separated ? "separated" : viewRefusal ?? "ineligible")); continue;
                    }
                    var outcome = Restore(row);
                    if (outcome == MountedRetainedRestoreOutcome.Restored) { Retained.Remove(row); continue; }
                    if (outcome == MountedRetainedRestoreOutcome.RetainedResidue) { Retained.Clear(); ResetSettle(); Latch = "retained residue"; return; }
                    if (expired) { Retained.Remove(row); Dropped.Add("clean refusal"); }
                }
                if (Retained.Count == 0) ResetSettle();
            }

            private void ResetSettle()
            {
                settledFrames = 0; settledSince = -1f;
                foreach (var row in Retained) row.EligibleSince = -1f;
            }

            // The integration update of one frame after the loader is idle: retained restoration, then pair validation.
            internal void Frame(float now) { Evaluate(now); Validate(); }

            // Frames of settled ordinary play, 0.1 s apart, starting after the given time.
            internal float Play(float from, int frames)
            {
                var now = from;
                for (var i = 0; i < frames; i++) { now += 0.1f; Frame(now); }
                return now;
            }
        }

        private static MountedAreaTransferLifecycle FullLoadTransfer()
        {
            var transfer = new MountedAreaTransferLifecycle(MountedAreaTransferRoute.FullLoad);
            transfer.ObserveSuspended();
            TestRunner.True(transfer.ObserveFullLoadPlacement(), "The full-load placement was not this transfer's completion.");
            return transfer;
        }

        // Mounted in a local area, carried over the world map (every unit put to sleep there), arrived in the next area
        // inside the native loading process with the party's views still inactive.
        private static Harness ArriveFromWorldMap(out Pair pair, out Row row, bool consultViews = true, bool partySelector = false,
            float riderX = 0f, float mountX = 1f, string arrivalMode = "Default")
        {
            var harness = new Harness { ConsultViews = consultViews };
            pair = harness.Add();
            harness.MountByPlayer(pair);
            row = harness.Suspend(pair);
            pair.Rider.Sleep(); pair.Mount.Sleep();
            // Native arrival gives every unit its own formation slot.
            pair.Rider.X = riderX; pair.Mount.X = mountX;
            harness.Loading = true; harness.PartySelector = partySelector; harness.Mode = arrivalMode;
            harness.Arrive(FullLoadTransfer());
            harness.Mode = "Default";
            return harness;
        }

        private static void ReadinessIsTheSettledInvariant()
        {
            for (var bits = 0; bits < 32; bits++)
            {
                bool riderSelf = (bits & 1) != 0, mountSelf = (bits & 2) != 0, riderHierarchy = (bits & 4) != 0, mountHierarchy = (bits & 8) != 0;
                var selector = (bits & 16) != 0;
                var refusal = MountedRestoreViewReadinessPolicy.Refusal(riderSelf, mountSelf, riderHierarchy, mountHierarchy, selector);
                // Stated independently of the implementation: all four values active, or the native party selector overlay.
                TestRunner.Equal(selector || (bits & 15) == 15, refusal == null, "Observation " + bits + " was misjudged.");
                // And it can never drift from the invariant: attached if and only if the next check in Default would keep it.
                TestRunner.Equal(MountedViewActivityPolicy.IsAdmissible(MountedGameModePolicy.Classify("Default"), riderSelf, mountSelf,
                    riderHierarchy, mountHierarchy, selector), refusal == null, "Restore readiness and the invariant disagree for observation " + bits + ".");
                if (refusal != null) TestRunner.Equal(MountedRestoreViewReadinessPolicy.NotReady, refusal, "The refusal text changed.");
            }
            // The recorded observation: riderSelf=False;mountSelf=False;riderHierarchy=False;mountHierarchy=False;nativePartySelectorActive=False.
            TestRunner.True(MountedRestoreViewReadinessPolicy.Refusal(false, false, false, false, false) != null, "The recorded observation was ready.");
        }
        private static void PriorDecisionReproducesRecordedDismount()
        {
            // Prior call shape (no view term): attached inside the loading process, log 2 lines 11187-11193.
            var prior = ArriveFromWorldMap(out var pair, out _, consultViews: false);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 1 && prior.Retained.Count == 0,
                "The prior decision did not attach at arrival, so it does not reproduce the recorded order.");
            // The loader goes idle; the first pair validation runs before any Default tick has woken the party
            // (lines 11212-11214: mode=Default, all four activity values False, trigger=CompanionInvalidated).
            prior.Loading = false;
            prior.Validate();
            TestRunner.True(!pair.Mounted, "The prior decision kept the pair mounted on inactive views.");
            TestRunner.Equal((CleanupTrigger?)CleanupTrigger.CompanionInvalidated, pair.LastTrigger, "The recorded trigger was not reproduced.");
            // Nothing remounts it afterwards: the pair stays dismounted until the player mounts again (line 11230).
            pair.Rider.Wake(); pair.Mount.Wake();
            prior.Play(0f, 50);
            TestRunner.True(!pair.Mounted && pair.SavedRestores == 1, "The prior flow remounted on its own.");

            // The same arrival with the readiness term: nothing is attached, so there is nothing to invalidate.
            var current = ArriveFromWorldMap(out var deferred, out var row);
            TestRunner.True(!deferred.Mounted && deferred.SavedRestores == 0 && deferred.Runtime.Attach == 1,
                "A pair was attached to inactive views at arrival.");
            TestRunner.True(current.Retained.Count == 1 && ReferenceEquals(current.Retained[0], row), "The deferred pair lost its retained custody.");
            current.Loading = false;
            current.Validate();
            TestRunner.True(deferred.LastTrigger == CleanupTrigger.AreaSuspension, "A deferred pair was invalidated.");
        }

        private static void RecordedOrdering()
        {
            var harness = ArriveFromWorldMap(out var pair, out var row);
            var ownerId = row.OwnerId; var generation = row.Generation;
            // The rest of the loading process (resource preload, the after-entry autosave, AreaLoadingComplete): the
            // custody holds, whatever the views do, and nothing validates because nothing is mounted.
            for (var i = 0; i < 20; i++) harness.Frame(i * 0.1f);
            TestRunner.True(!pair.Mounted && harness.Retained.Count == 1, "Restoration ran inside the loading process.");
            // The loader is idle. The integration update of that frame runs before the first Default tick.
            harness.Loading = false;
            harness.Frame(2.0f);
            TestRunner.True(!pair.Mounted && harness.Retained.Count == 1 && pair.SavedRestores == 0, "A pair was attached before its views were active.");
            // The first Default tick: native SleepingUnitsController wakes the party and its views activate.
            pair.Rider.Wake(); pair.Mount.Wake();
            // Ordinary play must hold for the settle bound before the single attachment.
            harness.Frame(2.1f); harness.Frame(2.2f);
            TestRunner.True(!pair.Mounted, "Restoration did not wait for settled ordinary play.");
            harness.Play(2.2f, 4);
            TestRunner.True(pair.Mounted, "The pair was not restored once its exact views were active in settled play.");
            TestRunner.Equal(1, pair.SavedRestores, "The pair was not attached exactly once.");
            TestRunner.Equal(2, pair.Runtime.Attach, "The attachment count is not the voluntary mount plus one restoration.");
            TestRunner.Equal(1, pair.VoluntaryMounts, "Restoration used a voluntary mount.");
            TestRunner.True(pair.Owner != null && pair.Owner.Id == ownerId && pair.Owner.Generation == generation &&
                pair.Owner.State == MountedPairOwnershipState.Active, "The restored pair did not keep its exact owner identity and generation.");
            TestRunner.True(ReferenceEquals(pair.CapturedRider, pair.Rider.View) && ReferenceEquals(pair.CapturedMount, pair.Mount.View),
                "The attachment did not capture the exact active views.");
            TestRunner.Equal(0, harness.Retained.Count, "A restored pair stayed in retained custody.");
            // It remains valid: the validation of the same frame and of every later frame passes.
            harness.Play(3f, 200);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 1 && pair.Runtime.Attach == 2 && harness.Latch == null && harness.Dropped.Count == 0,
                "The restored pair did not stay mounted and valid.");
        }

        private static void Idempotence()
        {
            var harness = new Harness();
            var pair = harness.Add();
            harness.MountByPlayer(pair);
            harness.Suspend(pair);
            pair.Rider.Sleep(); pair.Mount.Sleep();
            harness.Loading = true;
            var transfer = FullLoadTransfer();
            // A repeated placement callback and a repeated restore request of the same transfer attempt nothing more.
            TestRunner.True(!transfer.ObserveFullLoadPlacement(), "A second placement was accepted for one transfer.");
            harness.Arrive(transfer); harness.Arrive(transfer); harness.Arrive(transfer);
            TestRunner.True(transfer.ResumeAttempted && harness.Retained.Count == 1 && !pair.Mounted, "A repeated arrival changed the deferred pair.");
            harness.Loading = false;
            // The views flicker ready and not ready; each evaluation reads them afresh and at most one attachment happens.
            var now = 0f;
            for (var i = 0; i < 12; i++)
            {
                if (i % 2 == 0) { pair.Rider.Wake(); pair.Mount.Wake(); } else pair.Mount.Sleep();
                now += 0.1f; harness.Evaluate(now);
            }
            pair.Rider.Wake(); pair.Mount.Wake();
            now = harness.Play(now, 10);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 1, "Repeated readiness notifications did not end in exactly one attachment.");
            // Later readiness evaluations, a later arrival callback of the finished transfer and a new transfer with
            // nothing retained change nothing.
            harness.Arrive(transfer); harness.Arrive(FullLoadTransfer());
            harness.Play(now, 100);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 1 && pair.Runtime.Attach == 2 && pair.Runtime.Authority == 2,
                "A restored pair was attached again.");
            TestRunner.Equal(1, harness.Registry.Owners.Length, "A repeated notification left a second owner.");
        }

        private static void Cancellation()
        {
            // Explicit dismount inside the settle window. The arrival is deferred and the views are awake, but ordinary
            // play has not yet held for the settle bound, so the row is still pending when the player mounts the pair
            // by hand and dismounts it again. The retained intent of that pair ends at the dismount.
            var harness = ArriveFromWorldMap(out var pair, out _);
            harness.Loading = false;
            pair.Rider.Wake(); pair.Mount.Wake();
            harness.Frame(0.1f);
            TestRunner.True(!pair.Mounted && harness.Retained.Count == 1, "Restoration did not wait for the settle bound.");
            harness.MountByPlayer(pair, 2);
            harness.DismountByPlayer(pair);
            TestRunner.Equal(0, harness.Retained.Count, "An explicit dismount left the pair's delayed restoration pending.");
            harness.Play(0.1f, 100);
            TestRunner.True(!pair.Mounted && pair.SavedRestores == 0, "A pair the player dismounted was re-attached by a stale delayed restoration.");
            // Mounting in a pause is not a route: production admits a mount in Default only.
            TestRunner.True(!MountedGameModePolicy.CanAdmitMountedAction("Pause") && MountedGameModePolicy.CanAdmitMountedAction("Default"),
                "The mount admission modes changed; the explicit-dismount cases need to be reviewed.");

            // Explicit dismount while a staged pair's intent is held through an encounter: the player mounts with the
            // voluntary combat control and dismounts again during the fight. Nothing remounts the pair afterwards.
            var staged = new Harness { Kind = MountedRetainedIntentKind.ScriptedEvent };
            var actor = staged.Add();
            staged.MountByPlayer(actor);
            staged.Suspend(actor);
            staged.Combat = true;
            staged.Frame(0.1f);
            TestRunner.True(!actor.Mounted && staged.Retained.Count == 1, "Scene-staged intent was not held through the encounter.");
            staged.MountByPlayer(actor, 2);
            staged.DismountByPlayer(actor);
            TestRunner.Equal(0, staged.Retained.Count, "An explicit combat dismount left the held intent pending.");
            staged.Combat = false;
            staged.Play(0.1f, 100);
            TestRunner.True(!actor.Mounted && actor.SavedRestores == 0, "A pair dismounted during the fight was remounted after it.");
            // Control: the same fight without the dismount. The player's own mount stands and the stale row is dropped.
            var kept = new Harness { Kind = MountedRetainedIntentKind.ScriptedEvent };
            var keeper = kept.Add();
            kept.MountByPlayer(keeper);
            kept.Suspend(keeper);
            kept.Combat = true;
            kept.Frame(0.1f);
            kept.MountByPlayer(keeper, 2);
            kept.Combat = false;
            kept.Play(0.1f, 100);
            TestRunner.True(keeper.Mounted && keeper.SavedRestores == 0 && kept.Retained.Count == 0, "The player's own combat mount was replaced or duplicated.");            // Only an explicit dismount releases; the mod's own suspensions and cleanups never do.
            foreach (CleanupTrigger trigger in Enum.GetValues(typeof(CleanupTrigger)))
                TestRunner.Equal(trigger == CleanupTrigger.Manual, MountedRetainedIntentPolicy.ReleasedByDismount(trigger),
                    trigger + " was misjudged as an explicit dismount.");

            // Combat before the views are ready: world-map intent never adopts an encounter.
            var fight = ArriveFromWorldMap(out var fighter, out _);
            fight.Loading = false; fight.Combat = true;
            fight.Frame(0.1f);
            fighter.Rider.Wake(); fighter.Mount.Wake();
            fight.Combat = false;
            fight.Play(0.1f, 100);
            TestRunner.True(!fighter.Mounted && fighter.SavedRestores == 0 && fight.Retained.Count == 0, "Restoration survived an encounter.");

            // A load replaces the world while the restoration is pending.
            var replaced = ArriveFromWorldMap(out var old, out _);
            replaced.Loading = false;
            replaced.World = new object();
            old.Rider.Wake(); old.Mount.Wake();
            replaced.Play(0f, 100);
            TestRunner.True(!old.Mounted && old.SavedRestores == 0 && replaced.Retained.Count == 0, "A delayed restoration crossed into a replaced world.");

            // A new area transfer begins while the restoration is pending: it owns the boundary, nothing restores under
            // it, and the pair is decided again at that transfer's own placement, once.
            var travel = ArriveFromWorldMap(out var traveller, out _);
            travel.Loading = false;
            travel.Frame(0.1f);
            travel.BoundaryOwned = true; travel.Loading = true;
            traveller.Rider.Wake(); traveller.Mount.Wake();
            travel.Play(0.1f, 50);
            TestRunner.True(!traveller.Mounted && travel.Retained.Count == 1, "A delayed restoration attached under a newer area transfer.");
            var next = FullLoadTransfer();
            travel.Arrive(next);
            TestRunner.True(traveller.Mounted && traveller.SavedRestores == 1 && travel.Retained.Count == 0,
                "The newer transfer's arrival did not decide the carried pair itself.");
            travel.BoundaryOwned = false; travel.Loading = false;
            travel.Arrive(next);
            travel.Play(5f, 100);
            TestRunner.True(traveller.Mounted && traveller.SavedRestores == 1, "The pair was attached again after the newer transfer restored it.");
        }

        private static void StaleIdentity()
        {
            // Replaced views: readiness is read from the exact current views at each evaluation and never remembered.
            var harness = ArriveFromWorldMap(out var pair, out _);
            harness.Loading = false;
            var oldRider = pair.Rider.View; var oldMount = pair.Mount.View;
            pair.Rider.View = new View { Self = false, Hierarchy = false };
            pair.Mount.View = new View { Self = false, Hierarchy = false };
            oldRider.Self = oldRider.Hierarchy = oldMount.Self = oldMount.Hierarchy = true;
            var now = harness.Play(0f, 30);
            TestRunner.True(!pair.Mounted && pair.SavedRestores == 0, "Readiness of replaced views revived the pair.");
            pair.Rider.Wake(); pair.Mount.Wake();
            now = harness.Play(now, 10);
            TestRunner.True(pair.Mounted && ReferenceEquals(pair.CapturedRider, pair.Rider.View) && !ReferenceEquals(pair.CapturedRider, oldRider),
                "The attachment did not capture the current views.");
            // A view replaced after the attachment is the established view invalidation, not a readiness question.
            pair.Rider.View = new View();
            harness.Play(now, 5);
            TestRunner.True(!pair.Mounted && pair.LastTrigger == CleanupTrigger.ViewReplaced && pair.SavedRestores == 1, "A replaced view after attachment was tolerated or restored.");

            // A removed actor: the row is dropped, even if a ready view object survives it.
            var removed = ArriveFromWorldMap(out var gone, out _);
            removed.Loading = false;
            gone.Mount.Present = false;
            gone.Rider.Wake(); gone.Mount.Wake();
            removed.Play(0f, 20);
            TestRunner.True(!gone.Mounted && gone.SavedRestores == 0 && removed.Retained.Count == 0, "A pair with a removed actor was revived.");

            // Another world: the policy cancels, and the real registry also refuses the identity of the old world.
            var registry = new MountedPairRegistry<Actor, object>(a => a.Id);
            var world = new object(); var rider = new Actor(); var mount = new Actor(); var other = new Actor(); var otherMount = new Actor();
            TestRunner.True(registry.TryReserve(world, "campaign", other, otherMount, 1, new object(), () => false, () => true, () => true, out var live, out var liveRefusal), liveRefusal);
            registry.Commit(live);
            TestRunner.True(!registry.TryImport(new object(), "campaign", Guid.NewGuid(), 3, rider, mount, new object(), true, () => false, () => true, () => true,
                out _, out var worldRefusal) && worldRefusal != null, "An identity from another world was imported.");
            TestRunner.Equal(MountedRetainedIntentDecision.Cancel, MountedRetainedIntentPolicy.Decide(MountedRetainedIntentKind.WorldMapTravel, false, false,
                false, "Default", false, false, false, false), "Retained intent of another world was kept.");

            // An obsolete pair generation: the player mounted the pair again (a newer owner now holds these actors), so
            // the retained row is dropped; its captured identity can never displace the live owner.
            var obsolete = ArriveFromWorldMap(out var remounted, out var staleRow);
            obsolete.Loading = false;
            remounted.Rider.Wake(); remounted.Mount.Wake();
            obsolete.MountByPlayer(remounted, staleRow.Generation + 1);
            var liveOwner = remounted.Owner;
            obsolete.Play(0f, 20);
            TestRunner.True(remounted.Mounted && remounted.SavedRestores == 0 && obsolete.Retained.Count == 0 && ReferenceEquals(remounted.Owner, liveOwner),
                "An obsolete retained identity replaced or duplicated the pair's current owner.");
            TestRunner.True(!obsolete.Registry.TryImport(obsolete.World, Harness.Campaign, staleRow.OwnerId, staleRow.Generation, remounted.Rider, remounted.Mount,
                remounted, true, () => false, () => true, () => true, out _, out var staleRefusal) && staleRefusal != null,
                "The registry imported an obsolete identity over a live owner.");
            TestRunner.True(!obsolete.Registry.TryImport(obsolete.World, Harness.Campaign, Guid.NewGuid(), 0, new Actor(), new Actor(), new object(), true,
                () => false, () => true, () => true, out _, out _), "A non-positive generation was imported.");
        }

        private static void GenuineInvalidationUnchanged()
        {
            // The invariant table itself is unchanged by this repair.
            foreach (var mode in new[] { "Default", "Pause", "FullScreenUi", "EscMode", "Dialog", "Cutscene", "GlobalMap", "Kingdom", "None" })
                for (var bits = 0; bits < 32; bits++)
                {
                    bool riderSelf = (bits & 1) != 0, mountSelf = (bits & 2) != 0, riderHierarchy = (bits & 4) != 0, mountHierarchy = (bits & 8) != 0, selector = (bits & 16) != 0;
                    var disposition = MountedGameModePolicy.Classify(mode);
                    var expected = disposition == MountedGameModeDisposition.PreserveNonWorldUi || disposition == MountedGameModeDisposition.PreserveScriptedScene ||
                        disposition == MountedGameModeDisposition.PreserveWorldInteraction && (selector || riderSelf && mountSelf && riderHierarchy && mountHierarchy);
                    TestRunner.Equal(expected, MountedViewActivityPolicy.IsAdmissible(disposition, riderSelf, mountSelf, riderHierarchy, mountHierarchy, selector),
                        "The view-activity invariant changed for " + mode + " observation " + bits + ".");
                }

            // A mounted pair in settled ordinary play whose view goes inactive is invalidated exactly as before, by each
            // of the four values, and that invalidation creates no delayed restoration.
            for (var value = 0; value < 4; value++)
            {
                var harness = new Harness();
                var pair = harness.Add();
                harness.MountByPlayer(pair);
                var now = harness.Play(0f, 20);
                TestRunner.True(pair.Mounted, "A valid pair was invalidated.");
                if (value == 0) pair.Rider.View.Self = false; else if (value == 1) pair.Mount.View.Self = false;
                else if (value == 2) pair.Rider.View.Hierarchy = false; else pair.Mount.View.Hierarchy = false;
                harness.Frame(now + 0.1f);
                TestRunner.True(!pair.Mounted && pair.LastTrigger == CleanupTrigger.CompanionInvalidated, "An inactive view in settled play was tolerated (value " + value + ").");
                TestRunner.Equal(0, harness.Retained.Count, "A genuine invalidation created retained intent.");
                pair.Rider.Wake(); pair.Mount.Wake();
                harness.Play(now + 0.1f, 200);
                TestRunner.True(!pair.Mounted && pair.SavedRestores == 0, "A genuinely invalidated pair was restored on its own.");
            }

            // A pair restored through the deferral gets no allowance afterwards either.
            var restored = ArriveFromWorldMap(out var later, out _);
            restored.Loading = false;
            later.Rider.Wake(); later.Mount.Wake();
            var time = restored.Play(0f, 10);
            TestRunner.True(later.Mounted, "The deferred pair was not restored.");
            later.Mount.Sleep();
            restored.Frame(time + 0.1f);
            TestRunner.True(!later.Mounted && later.LastTrigger == CleanupTrigger.CompanionInvalidated && restored.Retained.Count == 0,
                "A restored pair kept a view-activity allowance.");

            // The invariant's one Default-mode allowance, the native party selector overlay, is unchanged for a mounted
            // pair, and restoration follows it exactly as it did before the repair: under the overlay a pair attaches
            // and is kept; when the overlay closes over views that are still inactive, that is the genuine invalidation.
            var overlay = new Harness();
            var shown = overlay.Add();
            overlay.MountByPlayer(shown);
            overlay.PartySelector = true;
            shown.Rider.Sleep(); shown.Mount.Sleep();
            overlay.Play(0f, 20);
            TestRunner.True(shown.Mounted, "The native party-selector allowance no longer preserves a mounted pair.");
            var covered = ArriveFromWorldMap(out var under, out _, partySelector: true);
            TestRunner.True(under.Mounted && under.SavedRestores == 1 && covered.Retained.Count == 0, "Restoration under the overlay no longer follows the invariant.");
            covered.Loading = false;
            var covering = covered.Play(0f, 150);
            TestRunner.True(under.Mounted && under.SavedRestores == 1, "A pair restored under the overlay was not kept while the overlay stayed.");
            under.Rider.Wake(); under.Mount.Wake();
            covered.PartySelector = false;
            covered.Play(covering, 50);
            TestRunner.True(under.Mounted && under.SavedRestores == 1, "The pair was lost when the overlay closed over active views.");
            var uncovered = ArriveFromWorldMap(out var exposed, out _, partySelector: true);
            uncovered.Loading = false;
            uncovered.PartySelector = false;
            uncovered.Frame(0.1f);
            TestRunner.True(!exposed.Mounted && exposed.LastTrigger == CleanupTrigger.CompanionInvalidated && uncovered.Retained.Count == 0,
                "Inactive views after the overlay closed were tolerated or turned into a delayed restoration.");
        }
        private static void NeverReadyIsBounded()
        {
            var harness = ArriveFromWorldMap(out var pair, out _);
            harness.Loading = false;
            var now = 0f; var evaluations = 0; var releasedAt = -1f;
            while (now < 60f)
            {
                now += 0.1f; evaluations++;
                harness.Frame(now);
                TestRunner.True(!pair.Mounted && pair.SavedRestores == 0 && pair.Runtime.Attach == 1, "Something was attached to views that never became active.");
                TestRunner.True(harness.Latch == null, "Waiting for views latched a failure.");
                // Nothing is owned while waiting: the registry is empty, so saving, teardown and a manual mount stay admitted.
                TestRunner.True(harness.Registry.Owners.Length == 0 && harness.Registry.AllSaveReady() && harness.Registry.AllTeardownReady(),
                    "A waiting pair held ownership.");
                if (harness.Retained.Count == 0 && releasedAt < 0f) releasedAt = now;
            }
            TestRunner.True(releasedAt > 0f, "Views that never became active kept the restoration pending indefinitely.");
            // Released by the first settled evaluation after the patience bound, counted from the first settled evaluation.
            var bound = MountedRetainedIntentPolicy.SettleSeconds + MountedRetainedIntentPolicy.EligibilityPatienceSeconds;
            TestRunner.True(releasedAt > bound && releasedAt <= bound + 0.6f, "The release was not at the patience bound: " + releasedAt);
            TestRunner.True(harness.Dropped.Count == 1 && harness.Dropped[0] == MountedRestoreViewReadinessPolicy.NotReady, "The release did not name the view refusal.");
            // The player can simply mount again once the views are active.
            pair.Rider.Wake(); pair.Mount.Wake();
            TestRunner.True(harness.Registry.AdmissionRefusal(harness.World, Harness.Campaign, pair.Rider, pair.Mount) == null, "A released pair could not be mounted again.");
            harness.MountByPlayer(pair, 2);
            harness.Play(now, 20);
            TestRunner.True(pair.Mounted && pair.SavedRestores == 0, "The pair could not be mounted by the player after the release.");
        }

        private static void FailedCleanupKeepsLatch()
        {
            var harness = ArriveFromWorldMap(out var pair, out _);
            harness.Loading = false;
            // While the views are not ready no attempt is made, so no failure can be produced or hidden by the waiting.
            pair.Runtime.FailAttach = true; pair.Runtime.FailRollbackPresentation = true;
            var now = harness.Play(0f, 20);
            TestRunner.True(harness.Latch == null && pair.SavedRestores == 0 && pair.Runtime.Restore == 1, "A waiting pair reached the failing attachment.");
            pair.Rider.Wake(); pair.Mount.Wake();
            now = harness.Play(now, 10);
            TestRunner.Equal(1, pair.SavedRestores, "The restoration was not attempted exactly once.");
            TestRunner.True(harness.Latch != null, "A restoration that left owned state did not keep the protective latch.");
            TestRunner.True(pair.Coordinator.State != RelationshipState.Unmounted, "The injected rollback failure did not leave the real coordinator owning state.");
            TestRunner.Equal(0, harness.Retained.Count, "A latched failure kept retrying the restoration.");
            // Ready views, time and further evaluations never clear it; only load housekeeping does.
            harness.Play(now, 300);
            TestRunner.True(harness.Latch != null && pair.SavedRestores == 1, "The protective latch was cleared without its cleanup.");
            // Control: the same failure with a successful rollback is a clean refusal and latches nothing.
            var clean = ArriveFromWorldMap(out var rolledBack, out _);
            clean.Loading = false;
            rolledBack.Runtime.FailAttach = true;
            rolledBack.Rider.Wake(); rolledBack.Mount.Wake();
            clean.Play(0f, 10);
            TestRunner.True(clean.Latch == null && !rolledBack.Mounted && rolledBack.Coordinator.State == RelationshipState.Unmounted,
                "A fully rolled-back attempt was latched.");
        }

        private static void Isolation()
        {
            var harness = new Harness();
            var waiting = harness.Add(); var ready = harness.Add(); var bystanders = harness.Add();
            harness.MountByPlayer(waiting); harness.MountByPlayer(ready);
            var waitingRow = harness.Suspend(waiting); var readyRow = harness.Suspend(ready);
            // World-map mode put every unit to sleep; only the second pair's views are active again at placement.
            waiting.Rider.Sleep(); waiting.Mount.Sleep();
            harness.Loading = true;
            harness.Arrive(FullLoadTransfer());
            TestRunner.True(ready.Mounted && ready.SavedRestores == 1 && ready.Owner.Id == readyRow.OwnerId, "A ready pair did not restore at arrival because another pair waited.");
            TestRunner.True(!waiting.Mounted && harness.Retained.Count == 1 && ReferenceEquals(harness.Retained[0], waitingRow), "The waiting pair was not the only retained row.");
            harness.Loading = false;
            var now = harness.Play(0f, 30);
            TestRunner.True(ready.Mounted && ready.SavedRestores == 1 && ready.Runtime.Attach == 2 && ready.Runtime.Authority == 2 && ready.Runtime.Restore == 1,
                "The waiting pair changed another pair.");
            waiting.Rider.Wake(); waiting.Mount.Wake();
            now = harness.Play(now, 10);
            TestRunner.True(waiting.Mounted && waiting.SavedRestores == 1 && waiting.Owner.Id == waitingRow.OwnerId, "The waiting pair did not restore on its own.");
            TestRunner.True(ready.SavedRestores == 1 && ready.Runtime.Attach == 2 && ready.Runtime.Authority == 2, "The late restoration touched the other pair.");
            // An unmounted pair is never captured, attached or owned.
            TestRunner.True(!bystanders.Mounted && bystanders.SavedRestores == 0 && bystanders.VoluntaryMounts == 0 && bystanders.Runtime.Attach == 0 &&
                bystanders.Runtime.Authority == 0 && harness.Registry.FindOwner(bystanders.Rider) == null && harness.Registry.FindOwner(bystanders.Mount) == null,
                "An unmounted actor was touched by the delayed restoration.");
            // Every restoration is a saved restore of the captured identity: no voluntary mount admission (which is the
            // only path that could plan an encounter adoption or spend an action) was used, and each pair acquired
            // movement authority exactly once per attachment.
            TestRunner.True(waiting.VoluntaryMounts == 1 && ready.VoluntaryMounts == 1 && waiting.Runtime.Authority == 2, "A restoration mounted voluntarily.");
            TestRunner.True(new MountedRelationshipCoordinator(new CountingRuntime()).Mount(null, MountedRelationshipAdmission.SavedRestore).Succeeded == false,
                "A saved restoration was accepted as a voluntary mount.");
            // An explicit dismount of one pair releases only rows that share its actors.
            TestRunner.True(MountedRetainedIntentPolicy.SharesActor("r1", "m1", "r1", "m1") && MountedRetainedIntentPolicy.SharesActor("r1", "m1", "x", "r1") &&
                MountedRetainedIntentPolicy.SharesActor("r1", "m1", "m1", "y"), "A pair's own retained row was not recognised.");
            TestRunner.True(!MountedRetainedIntentPolicy.SharesActor("r1", "m1", "r2", "m2") && !MountedRetainedIntentPolicy.SharesActor("r1", "m1", null, null) &&
                !MountedRetainedIntentPolicy.SharesActor(null, null, null, null) && !MountedRetainedIntentPolicy.SharesActor("", "", "", ""),
                "Another pair's retained row, or an unknown actor, was released.");
        }

        private static void ArrivalPlacementGeometry()
        {
            // The native formation gives every unit its own slot. Rider and mount arrive nine metres apart, beyond the
            // six metres of the world-map retry. Nothing moved them, so the deferred pair restores as the arrival
            // restoration always did.
            var held = ArriveFromWorldMap(out var apart, out var heldRow, riderX: 0f, mountX: 9f);
            TestRunner.True(heldRow.ArrivalDeferred && !apart.Mounted, "The arrival placement of a deferred pair was not kept.");
            held.Loading = false;
            apart.Rider.Wake(); apart.Mount.Wake();
            held.Play(0f, 10);
            TestRunner.True(apart.Mounted && apart.SavedRestores == 1 && held.Dropped.Count == 0, "A deferred pair in its untouched arrival formation was refused for its separation.");

            // Something relocated the mount before the restoration: the pair is no longer the arrival placement, and
            // the ordinary rule applies (never snap a rider across the scene).
            var moved = ArriveFromWorldMap(out var relocated, out _, riderX: 0f, mountX: 9f);
            moved.Loading = false;
            relocated.Rider.Wake(); relocated.Mount.Wake();
            relocated.Mount.X = 12f;
            moved.Play(0f, 10);
            TestRunner.True(!relocated.Mounted && relocated.SavedRestores == 0 && moved.Dropped.Count == 1 && moved.Dropped[0] == "separated",
                "A relocated pair kept the arrival allowance.");
            // Relocated to within reach: the ordinary rule restores it.
            var near = ArriveFromWorldMap(out var gathered, out _, riderX: 0f, mountX: 9f);
            near.Loading = false;
            gathered.Rider.Wake(); gathered.Mount.Wake();
            gathered.Mount.X = 4f;
            near.Play(0f, 10);
            TestRunner.True(gathered.Mounted && gathered.SavedRestores == 1, "A relocated pair within reach was not restored by the ordinary rule.");
            // A drift within the tolerance is still the arrival placement.
            var drift = ArriveFromWorldMap(out var settled, out _, riderX: 0f, mountX: 9f);
            drift.Loading = false;
            settled.Rider.Wake(); settled.Mount.Wake();
            settled.Mount.X = 9.05f;
            drift.Play(0f, 10);
            TestRunner.True(settled.Mounted, "A unit within the placement tolerance was treated as relocated.");

            // A pair retained at arrival for any other reason (here the arrival mode) never gets the arrival geometry.
            var other = ArriveFromWorldMap(out var scene, out var otherRow, riderX: 0f, mountX: 9f, arrivalMode: "Cutscene");
            TestRunner.True(!otherRow.ArrivalDeferred && other.Retained.Count == 1, "A pair retained for another refusal was marked as deferred for its views.");
            other.Loading = false;
            scene.Rider.Wake(); scene.Mount.Wake();
            other.Play(0f, 10);
            TestRunner.True(!scene.Mounted && other.Dropped.Count == 1 && other.Dropped[0] == "separated", "A pair retained for another refusal was seated across nine metres.");
            // A newer transfer does not inherit an earlier arrival placement.
            var again = ArriveFromWorldMap(out var twice, out var twiceRow, riderX: 0f, mountX: 9f);
            again.BoundaryOwned = true;
            again.Mode = "Cutscene";
            again.Arrive(FullLoadTransfer());
            TestRunner.True(!twiceRow.ArrivalDeferred && !twice.Mounted, "An earlier arrival placement travelled into a newer transfer.");

            // The policy itself.
            var tolerance = MountedRetainedIntentPolicy.ArrivalPlacementToleranceMetres;
            TestRunner.True(MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, 0f, 0f) && MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, tolerance, tolerance),
                "An unmoved pair is not its arrival placement.");
            TestRunner.True(!MountedRetainedIntentPolicy.ArrivalPlacementHeld(false, 0f, 0f) && !MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, tolerance + 0.01f, 0f) &&
                !MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, 0f, tolerance + 0.01f) && !MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, float.NaN, 0f) &&
                !MountedRetainedIntentPolicy.ArrivalPlacementHeld(true, 0f, float.PositiveInfinity), "A moved, unknown or never-deferred pair kept the arrival placement.");
            MountedRetainedPairDecision Pair(float separation, bool sameArea = true, bool arrival = true, string views = null, bool expired = false) =>
                MountedRetainedIntentPolicy.DecidePair(true, true, false, false, null, separation, MountedRetainedIntentPolicy.MaximumRestoreSeparationMetres,
                    sameArea, expired, false, MountedRegroupState.Unavailable, false, views, arrival);
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(9f), "The held arrival placement was refused for its separation.");
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(9f, sameArea: false), "The held arrival placement was refused for its navigation area.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(9f, arrival: false), "The ordinary separation rule changed.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(float.NaN), "A non-finite separation was accepted for an arrival placement.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(9f, views: MountedRestoreViewReadinessPolicy.NotReady), "The arrival placement attached to inactive views.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(9f, views: MountedRestoreViewReadinessPolicy.NotReady, expired: true), "The arrival placement outlived its patience.");
        }

        private static void DepartingArrival()
        {
            TestRunner.Equal(MountedDepartingArrivalDecision.Restore, MountedRetainedIntentPolicy.DecideDepartingArrival(null, false, null),
                "An eligible departing pair with active views did not restore at arrival.");
            TestRunner.Equal(MountedDepartingArrivalDecision.Defer,
                MountedRetainedIntentPolicy.DecideDepartingArrival(null, false, MountedRestoreViewReadinessPolicy.NotReady),
                "An eligible departing pair was attached to inactive views.");
            // Every refusal keeps its established outcome whatever the views are.
            foreach (var views in new[] { null, MountedRestoreViewReadinessPolicy.NotReady })
            {
                TestRunner.Equal(MountedDepartingArrivalDecision.Fail,
                    MountedRetainedIntentPolicy.DecideDepartingArrival("Both units must be alive and conscious.", false, views),
                    "A refused departing pair no longer fails the all-pair restoration.");
                TestRunner.Equal(MountedDepartingArrivalDecision.Retire,
                    MountedRetainedIntentPolicy.DecideDepartingArrival(MountedAreaControlRule.ControlRefusal, true, views),
                    "The native capital rule no longer retires a departing pair.");
            }
        }

        private static void RefusalsPrecedeReadiness()
        {
            var notReady = MountedRestoreViewReadinessPolicy.NotReady;
            MountedCarriedArrivalDecision Arrive(bool combat = false, bool resolved = true, bool profile = true, bool permanent = false,
                string refusal = null, bool capital = false) =>
                MountedRetainedIntentPolicy.DecideCarriedArrival(combat, resolved, profile, permanent, refusal, capital, notReady);
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, Arrive(), "An eligible carried pair was attached to inactive views.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(combat: true), "Inactive views kept a carried pair through an arrival encounter.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(resolved: false), "Inactive views kept a pair with a missing actor.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(profile: false), "Inactive views kept a pair whose profile changed.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Drop, Arrive(permanent: true, refusal: "Both units must be alive and conscious."), "Inactive views kept a dead member.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Retire, Arrive(refusal: MountedAreaControlRule.ControlRefusal, capital: true),
                "Inactive views turned the native capital retirement into pending intent.");
            TestRunner.Equal(MountedCarriedArrivalDecision.Retain, Arrive(refusal: "Both stock movement agents must be enabled before mounting."),
                "A transient refusal with inactive views was not retained.");
            // The call shape without a view term keeps every earlier decision.
            TestRunner.Equal(MountedCarriedArrivalDecision.Restore, MountedRetainedIntentPolicy.DecideCarriedArrival(false, true, true, false, null),
                "The established arrival decision changed for a caller that passes no view term.");

            MountedRetainedPairDecision Pair(bool resolved = true, bool already = false, bool permanent = false, string refusal = null,
                float separation = 1f, bool sameArea = true, bool expired = false, bool staging = false, string views = null) =>
                MountedRetainedIntentPolicy.DecidePair(resolved, resolved, already, permanent, refusal, separation,
                    MountedRetainedIntentPolicy.MaximumRestoreSeparationMetres, sameArea, expired, staging, MountedRegroupState.Unavailable, false, views);
            TestRunner.Equal(MountedRetainedPairDecision.Restore, Pair(), "A ready eligible pair did not restore.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(views: notReady), "A pair with inactive views was attached or dropped early.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, expired: true), "A pair whose views never activated was kept past its patience.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, resolved: false), "Inactive views kept a removed actor.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, already: true), "Inactive views kept a pair that is already mounted.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, permanent: true), "Inactive views kept a dead member.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, separation: 7f), "Inactive views kept a separated pair.");
            TestRunner.Equal(MountedRetainedPairDecision.Drop, Pair(views: notReady, sameArea: false), "Inactive views kept a pair across a navmesh boundary.");
            TestRunner.Equal(MountedRetainedPairDecision.Retry, Pair(views: notReady, staging: true), "Scene staging of a pair with inactive views was judged.");
        }

        private static void BoundsAndRelease()
        {
            TestRunner.True(MountedRetainedIntentPolicy.SettleFrames == 5 && MountedRetainedIntentPolicy.SettleSeconds == 0.3f, "The settle bound changed.");
            TestRunner.True(!MountedRetainedIntentPolicy.Settled(4, 10f) && !MountedRetainedIntentPolicy.Settled(500, 0.29f) &&
                MountedRetainedIntentPolicy.Settled(5, 0.3f), "The settle bound is not five frames and 0.3 seconds together.");
            TestRunner.Equal(10f, MountedRetainedIntentPolicy.EligibilityPatienceSeconds, "The patience bound changed.");
            TestRunner.True(!MountedRetainedIntentPolicy.PatienceExpired(-1f, 1000f), "Patience expired while it was not counting.");
            TestRunner.True(!MountedRetainedIntentPolicy.PatienceExpired(5f, 15f) && MountedRetainedIntentPolicy.PatienceExpired(5f, 15.01f),
                "The patience bound is not exclusive at ten seconds.");
        }
    }
}
