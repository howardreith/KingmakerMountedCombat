using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // A pair owns only its relationship and local adapters. The world owns the
    // ledger, subscriptions, native controls and archive/transition operations.
    internal sealed class MountedPairServices
    {
        internal GameMountedRelationshipService Relationship;
        internal MountedCombatController Combat;
        internal MountedPlayerActionController PlayerActions;
        internal MountedPairTurnParticipant Participant;
        internal MountedPairCommandScheduler Scheduler;
        internal HorsePrimaryAttackAnimationAdapter HorseAnimation;
        internal MountedAnimationAdapter Animation;
        internal MountedDollRoomIkAdapter DollRoomIk;
        internal UnitEntityData AdmissionRider, AdmissionMount;
        internal object AdmissionSession;
        internal long UpdateFailureCount;
        internal string LastUpdateFailure, LastCleanupFailure;
    }

    internal sealed class MountedWorldService : IDisposable
    {
        private readonly IModLogger logger;
        private readonly DiagnosticSettings settings;
        private readonly List<MountedPairServices> pairs = new List<MountedPairServices>();
        private bool disposed;
        private bool constructingPair;
        private readonly List<FailedPairConstruction> failedConstructions = new List<FailedPairConstruction>();
        private Func<bool> admissionFenced = () => false;
        private readonly Func<bool> combinedAdmissionFence;
        internal Action<string> BeforePairConstructionStep { get; set; }
        internal int FailedConstructionCount => failedConstructions.Count;
        private string lastRetirementFailure;
        internal readonly MountedPairRegistry<UnitEntityData, GameMountedRelationshipService> Registry;
        internal readonly UnifiedMountedTurnCoordinator UnifiedTurn;
        internal MountedPairServices Primary => pairs[0];
        internal MountedPairServices[] Pairs => pairs.ToArray();
        internal NativeMountedControlService NativeControls { get; private set; }
        internal Func<bool> AdmissionFenced
        {
            get => combinedAdmissionFence;
            set => admissionFenced = value ?? throw new ArgumentNullException(nameof(value));
        }
        private bool IsAdmissionFenced() => constructingPair || failedConstructions.Count != 0 || admissionFenced();
        internal Func<bool> ArchiveWorkerRunning { get; set; } = () => false;
        internal event Action<GameMountedRelationshipService, CleanupTrigger> PairDismounting;
        internal int BoundaryCount => pairs.Count;

        internal MountedWorldService(IModLogger logger, DiagnosticSettings settings) : this(logger, settings, null) { }
        internal MountedWorldService(IModLogger logger, DiagnosticSettings settings, Action<string> beforeConstruction)
        {
            this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            BeforePairConstructionStep = beforeConstruction;
            combinedAdmissionFence = IsAdmissionFenced;
            Registry = new MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>(unit => unit.UniqueId);
            var primary = new MountedPairServices();
            constructingPair = true;
            try
            {
                CreateRelationship(primary);
                UnifiedTurn = new UnifiedMountedTurnCoordinator(primary.Relationship, settings, primary.Scheduler, logger);
                primary.Participant = UnifiedTurn.Primary;
                CompletePair(primary, primary.Participant);
                BeforePairConstructionStep?.Invoke("publish");
                pairs.Add(primary);
                UnifiedTurn.ParticipantUpdateFailed = (participant, error) => HandlePairFailure(Find(participant.Relationship), error);
            }
            catch (Exception error)
            {
                var failures = CleanupUnpublishedPair(primary);
                throw new AggregateException("Primary mounted services could not be constructed.", new[] { error }.Concat(failures));
            }
            finally { constructingPair = false; }
        }

        private void CreateRelationship(MountedPairServices pair)
        {
            BeforePairConstructionStep?.Invoke("relationship");
            pair.Relationship = new GameMountedRelationshipService(logger, settings, Registry) { World = this };
            BeforePairConstructionStep?.Invoke("scheduler");
            pair.Scheduler = new MountedPairCommandScheduler(pair.Relationship, settings, logger);
        }

        private MountedPairServices ConstructPair()
        {
            if (constructingPair || failedConstructions.Count != 0 ||
                pairs.Count >= MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>.MaximumOwners)
                throw new InvalidOperationException("Mounted world construction is retained or at capacity.");
            var pair = new MountedPairServices();
            constructingPair = true;
            try
            {
                CreateRelationship(pair);
                BeforePairConstructionStep?.Invoke("participant");
                pair.Participant = UnifiedTurn.CreateUnpublishedParticipant(pair.Relationship, pair.Scheduler);
                CompletePair(pair, pair.Participant);
                BeforePairConstructionStep?.Invoke("publish");
                UnifiedTurn.PublishParticipant(pair.Participant);
                pairs.Add(pair);
                return pair;
            }
            catch (Exception error)
            {
                var failures = CleanupUnpublishedPair(pair);
                if (failures.Length != 0)
                    failedConstructions.Add(new FailedPairConstruction { Pair = pair, Cause = error, Failures = failures });
                throw new AggregateException("Additional mounted services could not be constructed.", new[] { error }.Concat(failures));
            }
            finally { constructingPair = false; }
        }

        private Exception[] CleanupUnpublishedPair(MountedPairServices pair)
        {
            var failures = new List<Exception>();
            if (pair.Relationship != null) pair.Relationship.World = null;
            var names = new[] { "player-actions", "combat", "scheduler", "participant", "relationship" };
            var actions = new Action[] {
                () => pair.PlayerActions?.Dispose(), () => pair.Combat?.Dispose(), () => pair.Scheduler?.Dispose(),
                () => UnifiedTurn?.AbandonUnpublishedParticipant(pair.Participant), () => pair.Relationship?.Dispose()
            };
            for (var index = 0; index < actions.Length; index++)
            {
                try { BeforePairConstructionStep?.Invoke("rollback-" + names[index]); actions[index](); }
                catch (Exception failure) { failures.Add(failure); }
            }
            return failures.ToArray();
        }

        private void RetryFailedConstructionCleanup()
        {
            foreach (var failure in failedConstructions.ToArray())
            {
                var errors = CleanupUnpublishedPair(failure.Pair);
                if (errors.Length == 0) { failedConstructions.Remove(failure); continue; }
                failure.Failures = errors;
            }
        }
        private sealed class FailedPairConstruction
        {
            internal MountedPairServices Pair;
            internal Exception Cause;
            internal Exception[] Failures;
        }

        private void CompletePair(MountedPairServices pair, MountedPairTurnParticipant participant)
        {
            pair.Participant = participant;
            BeforePairConstructionStep?.Invoke("combat");
            pair.HorseAnimation = new HorsePrimaryAttackAnimationAdapter(pair.Relationship, logger);
            pair.Combat = new MountedCombatController(pair.Relationship, settings, pair.HorseAnimation,
                participant, pair.Scheduler, logger);
            participant.BindCombat(pair.Combat);
            pair.Animation = new MountedAnimationAdapter(pair.Relationship, pair.Combat, pair.HorseAnimation, logger);
            pair.DollRoomIk = new MountedDollRoomIkAdapter(pair.Relationship, logger);
            BeforePairConstructionStep?.Invoke("player-actions");
            pair.PlayerActions = new MountedPlayerActionController(pair.Relationship, settings, logger, pair.Combat);
            pair.Combat.ChargeArchiveWorkerRunning = () => ArchiveWorkerRunning();
            pair.Relationship.Dismounting += trigger => PairDismounting?.Invoke(pair.Relationship, trigger);
            pair.Relationship.BindOwnershipReadiness(participant.RetainsPairParticipation,
                () => pair.Combat.HasRetainedPairCommands || NativeControls?.HasUnsettledShellFor(pair.Relationship) == true,
                () => !participant.HasUnsettledPreparation && !pair.PlayerActions.HasVoluntaryTransitionInFlight &&
                    !pair.Combat.HasChargeOwnership && !pair.Combat.ChargeNativeBoundaryPending &&
                    NativeControls?.HasUnsettledShellFor(pair.Relationship) != true,
                () => !pair.Combat.ChargeAdmissionFenced && !participant.HasUnsettledPreparation &&
                    NativeControls?.HasUnsettledShellFor(pair.Relationship) != true);
        }

        internal void BindControls(NativeMountedControlService controls)
        {
            if (NativeControls != null || controls == null) throw new InvalidOperationException("World controls bind once.");
            NativeControls = controls;
        }

        internal MountedPairServices Find(GameMountedRelationshipService relationship) =>
            relationship == null ? null : pairs.FirstOrDefault(pair => ReferenceEquals(pair.Relationship, relationship));

        internal MountedPairServices ForActor(UnitEntityData actor)
        {
            if (actor == null) return null;
            var owned = Registry.FindOwner(actor);
            if (owned != null) return Find(owned.Boundary);
            var session = Game.Instance?.Player;
            return pairs.FirstOrDefault(pair => ReferenceEquals(pair.AdmissionSession, session) &&
                (ReferenceEquals(pair.AdmissionRider, actor) || ReferenceEquals(pair.AdmissionMount, actor)));
        }

        // Presentation may follow selection; admitted work uses its captured
        // boundary or ForActor and never this projection.
        internal MountedPairServices Selected
        {
            get
            {
                var selection = SelectionManager.Instance?.SelectedUnits;
                var actor = selection != null && selection.Count == 1 ? selection[0] : null;
                return ForActor(actor);
            }
        }

        internal GameMountedRelationshipService GetOrCreateAdmission(UnitEntityData rider, UnitEntityData mount) =>
            GetOrCreateAdmissionForSession(Game.Instance?.Player, rider, mount);

        // Session is observed once at the native boundary; detached contract probes
        // supply that external observation without constructing a Unity world.
        internal GameMountedRelationshipService GetOrCreateAdmissionForSession(object session, UnitEntityData rider, UnitEntityData mount)
        {
            if (disposed || constructingPair || failedConstructions.Count != 0 || rider == null || mount == null) return null;
            var owner = Registry.FindOwner(rider) ?? Registry.FindOwner(mount);
            if (owner != null) return owner.Boundary;
            if (!NativeMountedSaveFormat.ArchiveNativeSupportEnabled) return Primary.Relationship;
            if (session == null) return null;
            var existing = pairs.FirstOrDefault(candidate => ReferenceEquals(candidate.AdmissionSession, session) &&
                ReferenceEquals(candidate.AdmissionRider, rider) && ReferenceEquals(candidate.AdmissionMount, mount));
            if (existing != null) return existing.Relationship;
            if (AdmissionFenced()) return null;
            ReleaseSettledAdmissions();
            var pair = AcquireEmptyBoundary();
            if (pair == null) return null;
            pair.AdmissionSession = session; pair.AdmissionRider = rider; pair.AdmissionMount = mount;
            return pair.Relationship;
        }

        // Called only after the entire archive graph has validated and resolved.
        // It allocates adapters, never native resources, preparation or Mount.
        internal MountedPairServices CreateRestoredBoundary()
        {
            if (constructingPair || failedConstructions.Count != 0)
                throw new InvalidOperationException("Pair construction cleanup must settle before archive restoration.");
            var pair = AcquireEmptyBoundary();
            if (pair == null) throw new InvalidOperationException("No settled boundary is available for archive restoration.");
            // Prevent a second record from acquiring this boundary before import.
            pair.AdmissionSession = new object();
            return pair;
        }

        private MountedPairServices AcquireEmptyBoundary()
        {
            var pair = pairs.FirstOrDefault(candidate => candidate.AdmissionSession == null &&
                candidate.Relationship.PairOwner == null && candidate.Relationship.State == RelationshipState.Unmounted &&
                candidate.Relationship.Runtime.NoPreparedPairResidue && !candidate.Combat.HasRetainedPairCommands &&
                candidate.Participant.CanReleaseBoundary);
            if (pair != null) return pair;
            if (pairs.Count >= MountedPairRegistry<UnitEntityData, GameMountedRelationshipService>.MaximumOwners) return null;
            return ConstructPair();
        }

        internal void ReleaseSettledAdmissions()
        {
            try { Registry.RetireSettled(); lastRetirementFailure = null; }
            catch (Exception error)
            {
                // The registry retains a failed owner's indexes and attempts all
                // other owners. A bad local predicate must not tear down the world.
                var detail = error.ToString();
                if (lastRetirementFailure != detail) logger.Error("Mounted custody retirement retained a failed owner: " + detail);
                lastRetirementFailure = detail;
            }
            foreach (var pair in Pairs)
            {
                try
                {
                    pair.Relationship.RefreshRetiredOwnership();
                    if (pair.AdmissionSession == null || pair.Relationship.PairOwner != null ||
                        pair.Relationship.State != RelationshipState.Unmounted || !pair.Relationship.Runtime.NoPreparedPairResidue ||
                        pair.Combat.HasRetainedPairCommands || !pair.Participant.CanReleaseBoundary ||
                        NativeControls?.HasUnsettledShellFor(pair.Relationship) == true) continue;
                    pair.Participant.ReleaseSettledParticipation();
                    pair.Relationship.AdvanceIdleBoundaryGeneration();
                    pair.AdmissionRider = null; pair.AdmissionMount = null; pair.AdmissionSession = null;
                }
                catch (Exception error) { HandlePairFailure(pair, error); }
            }
        }

        internal void DiscardAdmissionBindings()
        {
            if (SerializationBusy || Registry.Owners.Any(owner => owner.State != MountedPairOwnershipState.Retired &&
                (owner.State != MountedPairOwnershipState.Detached || owner.RetainsObligations())))
                throw new InvalidOperationException("Old-world custody has not settled before admission discard.");
            NativeControls?.RetireWorldControlBindings();
            ReleaseSettledAdmissions();
        }

        internal Exception[] Visit(Action<MountedPairServices> action)
        {
            var errors = new List<Exception>();
            foreach (var pair in Pairs)
                try { action(pair); } catch (Exception error) { errors.Add(error); }
            return errors.ToArray();
        }

        internal bool DrainChargeOwnership(string reason)
        {
            var ready = true;
            var errors = Visit(pair => { if (!pair.Combat.TryDrainChargeOwnership(reason)) ready = false; });
            return ready && errors.Length == 0;
        }

        internal bool SerializationBusy => ArchiveWorkerRunning() || pairs.Any(pair => pair.Relationship.SaveSerializationSuspended);
        internal bool AnyChargeFenced => pairs.Any(pair => pair.Combat.ChargeAdmissionFenced);

        // An individual runtime failure services that owner's cleanup and keeps
        // healthy pairs running. Failed cleanup retains its registry and retries.
        internal void UpdatePairs()
        {
            RetryFailedConstructionCleanup();
            foreach (var pair in Pairs)
            {
                try { pair.Combat.Update(); pair.Relationship.ValidateActivePair(); }
                catch (Exception error) { HandlePairFailure(pair, error); }
            }
            ReleaseSettledAdmissions();
        }

        private void HandlePairFailure(MountedPairServices pair, Exception error)
        {
            if (pair == null) throw new InvalidOperationException("Pair update failed without its exact owner.", error);
            pair.UpdateFailureCount++;
            var message = error.ToString();
            if (pair.LastUpdateFailure != message)
            {
                pair.LastUpdateFailure = message;
                logger.Error("Mounted pair update failed; retained local cleanup: " + message);
            }
            try { pair.Combat.Cancel("pair update failure"); pair.Relationship.Dismount(CleanupTrigger.Exception); }
            catch (Exception cleanup)
            {
                var detail = cleanup.ToString();
                if (pair.LastCleanupFailure != detail) logger.Error("Mounted pair cleanup remains retained: " + cleanup);
                pair.LastCleanupFailure = detail;
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            RetryFailedConstructionCleanup();
            if (constructingPair || failedConstructions.Count != 0)
                throw new InvalidOperationException("Unpublished pair cleanup remains owned by this world.");
            if (!Registry.AllTeardownReady() || AnyChargeFenced || SerializationBusy || UnifiedTurn.HasUnsettledPreparation)
                throw new InvalidOperationException("World teardown retains an unfinished operation.");
            UnifiedTurn.EndPairedServiceParticipation();
            var errors = Visit(pair =>
            {
                var local = new List<Exception>();
                foreach (var service in new IDisposable[] { pair.PlayerActions, pair.Combat, pair.Scheduler, pair.Relationship })
                    try { service?.Dispose(); } catch (Exception error) { local.Add(error); }
                if (local.Count != 0) throw new AggregateException("Pair service teardown retained obligations.", local);
            });
            if (errors.Length != 0) throw new AggregateException("World pair teardown retains failed owners.", errors);
            UnifiedTurn.Dispose();
            disposed = true;
        }
    }
}
