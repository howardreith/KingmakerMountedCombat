using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Diagnostics;
using KingmakerMountedCombat.Domain;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedPairTurnParticipant
    {
        internal IEnumerable<UnitEntityData> ParticipationActors =>
            new[] { relationship.Rider, relationship.Mount, activation?.Principal, activation?.Partner, armedRider, armedMount, pendingSplitMount }
                .Where(u => u != null).Distinct();

        internal IEnumerable<UnitEntityData> PersistenceActors => ParticipationActors;

        internal bool HasUnsettledPreparation => nativePreparationCommands.Values.Any(command => command != null && !command.IsFinished) ||
            preparingConfusionActor != null || resumingContext != null || nativeConditionForfeitContext != null;

        internal SavedPairedState CaptureParticipation()
        {
            if (activation == null && armedRider == null && armedMount == null && pendingSplitMount == null) return null;
            var snapshot = activation?.Capture();
            return new SavedPairedState
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

        internal void RestoreParticipation(SavedPairedState pair, IDictionary<string, UnitEntityData> actors, bool turnBased)
        {
            DiscardParticipation();
            var controller = Game.Instance.TurnBasedCombatController;
            if (pair == null) return;
            armedRider = pair.RiderId == null ? null : actors[pair.RiderId];
            armedMount = pair.MountId == null ? null : actors[pair.MountId];
            activationSession = Game.Instance.Player;
            splitReleaseRound = pair.SplitReleaseRound;
            pendingSplitMount = pair.PendingSplitId == null ? null : actors[pair.PendingSplitId];
            pendingSplitRound = pair.PendingSplitRound;
            pairedRenewalNotBefore = pair.RenewalNotBeforeTicks;
            lastTurnBased = turnBased;
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

        // Retain the discard count for persistence observations. Managed callers and text are
        // forensic evidence for an exact selected persistence observer, not ordinary cleanup work.
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

        internal void DiscardParticipation()
        {
            PersistenceWorldDiscardCount++;
            // Never leave a previous captured trace looking like this discard's observation.
            LastPersistenceWorldDiscardObservation = "not-captured";
            try
            {
                if (RuntimeAutomationHost.ShouldCapturePersistenceWorldDiscard(this))
                {
                    LastPersistenceWorldDiscardObservation = "discard;count=" + PersistenceWorldDiscardCount +
                        ";activation=" + (activation?.Identity ?? "none") + ";callers=" + DescribeManagedCallers();
                    logger.Info("Pair participation discarded: " + LastPersistenceWorldDiscardObservation);
                }
            }
            finally
            {
                // World replacement is housekeeping. End/forfeit and voluntary split
                // belong to gameplay transitions and must not run during this discard.
                // A forensic capture/logging failure propagates after real cleanup is attempted.
                DisposePartnerContext();
                nativePreparationCommands.Clear();
                activation = null; armedRider = null; armedMount = null; activationSession = null;
                preparingConfusionActor = null; preparedRiderTurn = null; resumingContext = null; nativeConditionForfeitContext = null;
                pendingSplitMount = null; pendingSplitRound = -1; splitReleaseRound = -1;
                pairedRenewalNotBefore = 0;
            }
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
