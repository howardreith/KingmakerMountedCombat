using System;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 2 (DOOR-01 / SPACE-01 in turn-based combat): the owner-seed exploration case's explicit opt-in stage. It runs
    // after the real-time door probe released the door and before the speed legs, with both pairs still mounted; the
    // encounter is the project's diagnostic target, started in the selected turn-based mode and ended by its removal.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string TurnBasedDoorRow = Chunk7TurnBasedDoorProbe.Row;
        private bool TurnBasedDoorSelected => OwnerSeed && request.OwnedPairs?.TurnBasedDoorSelected == true;
        private Chunk7TurnBasedDoorProbe turnBasedDoor;

        private void BeginTurnBasedDoor()
        {
            Require(TurnBasedDoorSelected && turnBasedDoor == null && doorProbe != null && doorProbe.Completed, "The turn-based door stage follows the completed real-time door probe once.");
            var door = doorProbe.Door;
            Require(door != null, "The real-time door probe has not released its door lease.");
            turnBasedDoor = new Chunk7TurnBasedDoorProbe(world, nativeScope, request, logger, SelectNativeMode, door, firstRider, firstMount, secondRider, secondMount, originalParty);
            evidence["turnBasedDoor"] = turnBasedDoor.Evidence;
            turnBasedDoor.Begin();
        }

        private bool PollTurnBasedDoor()
        {
            Require(turnBasedDoor != null, "No turn-based door stage is running.");
            if (!turnBasedDoor.Poll()) return false;
            foreach (var command in turnBasedDoor.IssuedCommands) if (!issuedCommands.Contains(command)) issuedCommands.Add(command);
            return true;
        }

        // The engine owns the native mode lease: the exploration's real-time selection is replaced by the turn-based
        // selection for the encounter and re-selected afterwards; cleanup disposes whichever lease is current.
        private void SelectNativeMode(bool turnBased)
        {
            mode?.Dispose(); mode = new NativeModeTransitionProbe(turnBased); mode.DispatchTemporaryValueIfRequired();
            Require(mode.TemporaryValueIsCurrent, "Selected native mode must be current before the next step.");
        }
    }
}
