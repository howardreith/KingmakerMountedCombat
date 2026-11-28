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
            // R11-F1 (round 12): completed step travel settles to its steered displacement before it is saved.
            record.SettleStepTravel(pair.Key.Position);
            return new SavedMovementAllocation
            {
                ActorId = pair.Key.UniqueId, Round = record.Round, RoundStartTicks = record.RoundStart,
                GrantId = record.GrantIdentity, Prepared = record.Prepared,
                MoveObserved = record.MoveUsed, StandardCommitted = record.StandardUsed,
                Movement = MountedMovementPersistenceRows.Capture(record.Movement)
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
                    Movement = MountedMovementPersistenceRows.Restore(row.Movement)
                });
        }
    }

    // PR #70 R11-F1 (round 12): the step commitment of one saved allocation row. The versioned save shape has exactly one step field
    // (SavedMovementValues.MetresStepped; MountedArchiveCodec requires the exact field set, so a new field would invalidate existing saves),
    // and the paired allocation keeps two step limits (the installed counter in nominal metres, the settled physical travel). The row carries
    // the larger of the two and restores both from it: the restored remainder min((1.524 - s) / CurrentSpeedMps, (1.524 - s) / MaxSpeed)
    // equals the saved one whenever the binding limit is the larger commitment (a straight step at any speed, any slower-than-nominal step)
    // and is never larger otherwise; an older row (one value, the same field) restores both limits from it.
    internal static class MountedMovementPersistenceRows
    {
        internal static SavedMovementValues Capture(MountedMovementState movement) => new SavedMovementValues
        {
            TimeMoved = movement.TimeMoved, TimeForced = movement.TimeForced, TimeStepped = movement.TimeStepped,
            MetresStepped = Math.Max(movement.MetresStepped, movement.MetresStepTravelled), StepImmune = movement.StepImmune,
            AiStep = movement.AiStep, AutoStop = movement.AutoStopPending
        };

        internal static MountedMovementState Restore(SavedMovementValues saved) => new MountedMovementState
        {
            TimeMoved = saved.TimeMoved, TimeForced = saved.TimeForced, TimeStepped = saved.TimeStepped,
            MetresStepped = saved.MetresStepped, MetresStepTravelled = saved.MetresStepped,
            StepImmune = saved.StepImmune, AiStep = saved.AiStep, AutoStopPending = saved.AutoStop
        };
    }
}
