using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Integration
{
    internal sealed partial class MountedMovementStateAdapter
    {
        internal IEnumerable<UnitEntityData> PersistenceActors => allocations.Keys;

        internal SavedMovementAllocation[] CapturePersistence() => allocations.Select(pair =>
        {
            var record = pair.Value;
            // R11-F1 (round 12): completed step travel settles to its steered displacement before it is saved. Round 13: the native counter and
            // the settled physical travel are captured independently (the travel on the row's StepTravelled, which archive schema 4 stores).
            record.SettleStepTravel(pair.Key.Position);
            return new SavedMovementAllocation
            {
                ActorId = pair.Key.UniqueId, Round = record.Round, RoundStartTicks = record.RoundStart,
                GrantId = record.GrantIdentity, Prepared = record.Prepared,
                MoveObserved = record.MoveUsed, StandardCommitted = record.StandardUsed,
                Movement = MountedMovementPersistenceRows.Capture(record.Movement),
                StepTravelled = MountedMovementPersistenceRows.CaptureTravel(record.Movement)
            };
        }).ToArray();

        internal void RestorePersistence(SavedMovementAllocation[] saved, IDictionary<string, UnitEntityData> actors)
        {
            // Bind lifetime first. A subsequent ordinary Maintain must not mistake
            // the imported records for old-world records and erase commitments.
            Clear();
            var game = Game.Instance;
            lifetime.SynchronizeSession(game.Player, game.Player.GameId);
            foreach (var row in saved)
                allocations.Add(actors[row.ActorId], new Allocation
                {
                    Controller = game.TurnBasedCombatController, Round = row.Round, RoundStart = row.RoundStartTicks,
                    GrantIdentity = row.GrantId, Prepared = row.Prepared,
                    MoveUsed = row.MoveObserved, StandardUsed = row.StandardCommitted,
                    Movement = MountedMovementPersistenceRows.Restore(row.Movement, row.StepTravelled)
                });
        }
    }

    // PR #70 R11-F1 (rounds 12-13): the step commitments of one saved allocation row. The paired allocation keeps two independent step limits:
    // the installed counter (nominal metres, MetresStepped, the value native's own turn context holds) and the settled physical travel
    // (MetresStepTravelled). The row's MetresStepped is the exact native counter and the travel is captured separately (StepTravelled on the
    // allocation row, stored by archive schema 4 in MountedArchiveData.StepTravel); restoration sets both exactly. A row read from a historical
    // format (archive schema 3, legacy schema 1-2) stored one value only: both limits are restored from it (its legacy interpretation; a
    // round-12 schema-3 writer stored the larger commitment, earlier writers the native counter). The legacy schema-2 writer (selected only
    // without the all-pair archive) still has one field: it stores the larger commitment (FoldForSingleFieldFormat), never a smaller one.
    internal static class MountedMovementPersistenceRows
    {
        internal static SavedMovementValues Capture(MountedMovementState movement) => new SavedMovementValues
        {
            TimeMoved = movement.TimeMoved, TimeForced = movement.TimeForced, TimeStepped = movement.TimeStepped,
            MetresStepped = movement.MetresStepped, StepImmune = movement.StepImmune,
            AiStep = movement.AiStep, AutoStop = movement.AutoStopPending
        };

        internal static float? CaptureTravel(MountedMovementState movement) => movement.MetresStepTravelled;

        internal static MountedMovementState Restore(SavedMovementValues saved, float? travelled) => new MountedMovementState
        {
            TimeMoved = saved.TimeMoved, TimeForced = saved.TimeForced, TimeStepped = saved.TimeStepped,
            MetresStepped = saved.MetresStepped, MetresStepTravelled = travelled ?? saved.MetresStepped,
            StepImmune = saved.StepImmune, AiStep = saved.AiStep, AutoStopPending = saved.AutoStop
        };

        // The single-field legacy format (schema 2): the larger commitment, so a restored remainder is never larger than the saved one.
        internal static void FoldForSingleFieldFormat(SavedMovementAllocation[] rows)
        {
            if (rows == null) return;
            foreach (var row in rows)
                if (row?.Movement != null && row.StepTravelled.HasValue)
                {
                    row.Movement.MetresStepped = Math.Max(row.Movement.MetresStepped, row.StepTravelled.Value);
                    row.StepTravelled = null;
                }
        }
    }
}
