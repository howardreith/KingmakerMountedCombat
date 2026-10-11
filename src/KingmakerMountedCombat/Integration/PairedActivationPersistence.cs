using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class UnifiedMountedTurnCoordinator
    {
        internal IEnumerable<UnitEntityData> PersistenceActors => movementState.PersistenceActors
            .Concat(new[] { activation?.Principal, activation?.Partner, armedRider, armedMount, pendingSplitMount })
            .Where(u => u != null).Distinct();

        internal bool HasUnsettledPreparation => nativePreparationCommands.Values.Any(command => command != null && !command.IsFinished) ||
            preparingConfusionActor != null || resumingContext != null;

        internal void CapturePersistence(SavedCombatData saved)
        {
            var snapshot = activation?.Capture();
            saved.Allocations = movementState.CapturePersistence();
            saved.Paired = new SavedPairedState
            {
                RiderId = (activation?.Principal ?? armedRider)?.UniqueId,
                MountId = (activation?.Partner ?? armedMount)?.UniqueId,
                Activation = snapshot == null ? null : new SavedActivation
                {
                    EncounterId = snapshot.EncounterId.ToString("N"), Sequence = snapshot.Sequence,
                    Ending = snapshot.Ending, Finalized = snapshot.Finalized,
                    Split = snapshot.Split, Suspended = snapshot.Suspended,
                    Rider = CaptureParticipation(snapshot.Rider), Mount = CaptureParticipation(snapshot.Mount)
                },
                BoundaryIsCurrent = activation?.Boundary != null &&
                    ReferenceEquals(activation.Boundary, Game.Instance.TurnBasedCombatController.CurrentTurn),
                Boundary = activation?.Boundary == null ||
                    ReferenceEquals(activation.Boundary, Game.Instance.TurnBasedCombatController.CurrentTurn) ? null :
                    NativeTurnPersistence.Capture(activation.Boundary),
                Partner = NativeTurnPersistence.Capture(partnerContext),
                SplitReleaseRound = splitReleaseRound, PendingSplitId = pendingSplitMount?.UniqueId,
                PendingSplitRound = pendingSplitRound, RenewalNotBeforeTicks = pairedRenewalNotBefore
            };
        }

        internal void RestorePersistence(SavedCombatData saved, IDictionary<string, UnitEntityData> actors)
        {
            DiscardPersistenceWorld();
            var pair = saved.Paired;
            var controller = Game.Instance.TurnBasedCombatController;
            movementState.RestorePersistence(saved.Allocations, actors);
            if (pair == null) return;
            armedRider = pair.RiderId == null ? null : actors[pair.RiderId];
            armedMount = pair.MountId == null ? null : actors[pair.MountId];
            activationSession = Game.Instance.Player;
            splitReleaseRound = pair.SplitReleaseRound;
            pendingSplitMount = pair.PendingSplitId == null ? null : actors[pair.PendingSplitId];
            pendingSplitRound = pair.PendingSplitRound;
            pairedRenewalNotBefore = pair.RenewalNotBeforeTicks;
            lastTurnBased = saved.TurnBased;
            if (pair.Activation == null) return;
            var boundary = pair.BoundaryIsCurrent ? controller.CurrentTurn :
                NativeTurnPersistence.Restore(pair.Boundary, actors);
            try
            {
                activation = PairedActivation<UnitEntityData, TurnController>.Restore(
                    pair.Activation.ToSnapshot(), armedRider, armedMount, boundary);
                partnerContext = NativeTurnPersistence.Restore(pair.Partner, actors);
                preparedRiderTurn = pair.BoundaryIsCurrent ? boundary : null;
                // The allocation remains the authority for delegated motion.
                if (boundary != null) movementState.CopyGrantedMovementToContext(boundary);
                if (partnerContext != null) movementState.CopyGrantedMovementToContext(partnerContext);
                LastInitiativeObservation = "saved-activation-rebound;identity=" + activation.Identity +
                    ";sequence=" + activation.Sequence + ";no-prepare-or-end";
            }
            finally
            {
                // A completed/suspended old boundary is an identity marker only.
                // It must not stay subscribed alongside the native current turn.
                if (!pair.BoundaryIsCurrent) boundary?.Dispose();
            }
        }

        // Diagnostics only (preview.154 CM07-mount-load-rt measurement): how often and from where the
        // persistence world was discarded, and which activation it held at that moment. Nothing here
        // changes behaviour; the real-time cold load lost the rebound activation without a trace.
        internal long PersistenceWorldDiscardCount { get; private set; }
        internal string LastPersistenceWorldDiscardObservation { get; private set; } = "none";

        private static string DescribeManagedCallers()
        {
            var frames = new System.Diagnostics.StackTrace(2, false).GetFrames();
            if (frames == null) return "<none>";
            var names = new List<string>();
            foreach (var frame in frames)
            {
                var method = frame.GetMethod(); var type = method?.DeclaringType;
                if (method == null || type == null) continue;
                var name = type.FullName + "." + method.Name;
                if (name.StartsWith("MonoMod.", StringComparison.Ordinal) || name.Contains("DynamicMethodDefinition")) continue;
                names.Add(name);
                if (names.Count >= 10) break;
            }
            return names.Count == 0 ? "<none>" : string.Join(" < ", names.ToArray());
        }

        internal void DiscardPersistenceWorld()
        {
            PersistenceWorldDiscardCount++;
            LastPersistenceWorldDiscardObservation = "discard;count=" + PersistenceWorldDiscardCount +
                ";activation=" + (activation?.Identity ?? "none") + ";callers=" + DescribeManagedCallers();
            logger.Info("Persistence world discarded: " + LastPersistenceWorldDiscardObservation);
            // World replacement is housekeeping. End/forfeit and voluntary split
            // belong to gameplay transitions and must not run during this discard.
            DisposePartnerContext();
            nativePreparationCommands.Clear();
            movementState.Clear();
            activation = null; armedRider = null; armedMount = null; activationSession = null;
            preparingConfusionActor = null; preparedRiderTurn = null; resumingContext = null;
            pendingSplitMount = null; pendingSplitRound = -1; splitReleaseRound = -1;
            pairedRenewalNotBefore = 0;
        }

        private static SavedParticipation CaptureParticipation(PairedActorSnapshot actor) => actor == null ? null :
            new SavedParticipation
            {
                Granted = actor.Granted, Prepared = actor.Prepared, Ended = actor.Ended,
                StandardObserved = actor.StandardObserved, MoveObserved = actor.MoveObserved,
                SwiftObserved = actor.SwiftObserved, ForfeitRecorded = actor.ForfeitRecorded,
                ForfeitSettled = actor.ForfeitSettled, ForfeitAdded = actor.ForfeitStandardAdded
            };
    }
}
