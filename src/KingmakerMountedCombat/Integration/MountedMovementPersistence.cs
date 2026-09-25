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
            var movement = record.Movement;
            return new SavedMovementAllocation
            {
                ActorId = pair.Key.UniqueId, Round = record.Round, RoundStartTicks = record.RoundStart,
                GrantId = record.GrantIdentity, Prepared = record.Prepared,
                MoveObserved = record.MoveUsed, StandardCommitted = record.StandardUsed,
                Movement = new SavedMovementValues
                {
                    TimeMoved = movement.TimeMoved, TimeForced = movement.TimeForced, TimeStepped = movement.TimeStepped,
                    MetresStepped = movement.MetresStepped, StepImmune = movement.StepImmune,
                    AiStep = movement.AiStep, AutoStop = movement.AutoStopPending
                }
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
                    Movement = new MountedMovementState
                    {
                        TimeMoved = row.Movement.TimeMoved, TimeForced = row.Movement.TimeForced,
                        TimeStepped = row.Movement.TimeStepped, MetresStepped = row.Movement.MetresStepped,
                        StepImmune = row.Movement.StepImmune, AiStep = row.Movement.AiStep,
                        AutoStopPending = row.Movement.AutoStop
                    }
                });
        }
    }
}
