using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Captures one pre-operation mounted registration. Progression never grants a
    // replacement registration, clears actor allocations or retires unfinished
    // ownership. Native commands/processes settle before targeted service cleanup.
    internal sealed class Chunk7ProgressionPairCustody
    {
        private readonly MountedWorldService world;
        private readonly NativeMountedControlService controls;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService> owner;
        private readonly MountedPairServices pair;
        private readonly MountedPairOwner<UnitEntityData, GameMountedRelationshipService>[] neighbours;
        private readonly Action assertWorld;
        private readonly Func<bool> nativeBusy;
        private readonly List<string> cleanupErrors = new List<string>();
        private readonly JArray observations = new JArray();
        private readonly HashSet<UnitCommand> observedCommands = new HashSet<UnitCommand>();
        private bool targetSettled;
        internal Chunk7ProgressionPairCustody(MountedWorldService world, UnitEntityData rider, UnitEntityData mount,
            Action assertWorld, Func<bool> nativeBusy)
        {
            this.world = world ?? throw new ArgumentNullException(nameof(world));
            this.assertWorld = assertWorld ?? throw new ArgumentNullException(nameof(assertWorld));
            this.nativeBusy = nativeBusy ?? throw new ArgumentNullException(nameof(nativeBusy));
            assertWorld();
            controls = world.NativeControls ?? throw new InvalidOperationException("Native mounted controls are unavailable.");
            owner = world.Registry.FindActive(rider);
            Require(owner != null && ReferenceEquals(owner.Mount, mount) && world.Registry.IsActiveOwner(owner, rider, mount),
                "Progression cleanup requires one exact active native rider/mount registration.");
            pair = world.Pairs.Single(value => ReferenceEquals(value.Relationship, owner.Boundary));
            neighbours = world.Registry.Owners.Where(value => !ReferenceEquals(value, owner)).ToArray();
            Require(neighbours.Length == 1 && neighbours.All(value => value.State == MountedPairOwnershipState.Active &&
                value.Boundary.State == RelationshipState.Mounted), "The selected progression fixture requires one independent mounted control pair.");
            CaptureCommands();
            Require(CommandsSettled(owner.Rider) && CommandsSettled(owner.Mount) && observedCommands.All(CommandSettled) &&
                PairCommandsSettled() && !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation,
                "Progression may not start while its native pair commands or processes are unsettled.");
            Observe("before-native-progression");
        }
        internal bool TargetSettled => targetSettled;
        internal bool NativeCommandsSettled
        {
            get { CaptureCommands(); return CommandsSettled(owner.Rider) && CommandsSettled(owner.Mount) && observedCommands.All(CommandSettled); }
        }
        internal JObject Capture() => new JObject { ["ownerId"] = owner.Id.ToString("D"), ["generation"] = owner.Generation,
            ["riderId"] = owner.RiderId, ["mountId"] = owner.MountId, ["targetSettled"] = targetSettled,
            ["cleanupErrors"] = new JArray(cleanupErrors), ["observations"] = observations.DeepClone(),
            ["manualActorAllocationWrites"] = 0, ["manualPreparationCalls"] = 0, ["manualCompanionRemovals"] = 0 };
        internal void Observe(string label)
        {
            assertWorld(); RequireNeighbours(); CaptureCommands();
            Require(observations.Count < 96, "Progression custody observations exceeded their bound.");
            observations.Add(new JObject { ["label"] = label, ["originalOwnerPresent"] = world.Registry.Owns(owner),
                ["ownerState"] = owner.State.ToString(), ["relationshipState"] = pair.Relationship.State.ToString(),
                ["currentGeneration"] = pair.Relationship.MountedPairGeneration,
                ["retainsObligations"] = owner.RetainsObligations(), ["saveReady"] = owner.SaveReady(),
                ["teardownReady"] = owner.TeardownReady(), ["noPreparedPairResidue"] = pair.Relationship.Runtime.NoPreparedPairResidue,
                ["nativeCommandsSettled"] = CommandsSettled(owner.Rider) && CommandsSettled(owner.Mount) && observedCommands.All(CommandSettled),
                ["nativeControlsSettled"] = !controls.HasUnsettledShellFor(pair.Relationship),
                ["unrelatedOwners"] = new JArray(neighbours.Select(value => new JObject { ["id"] = value.Id.ToString("D"),
                    ["generation"] = value.Generation, ["riderObject"] = Id(value.Rider), ["mountObject"] = Id(value.Mount),
                    ["state"] = value.State.ToString(), ["relationshipState"] = value.Boundary.State.ToString() })) });
        }
        internal bool TryDrainOriginalPair()
        {
            assertWorld();
            if (targetSettled) { RequireNeighbours(); return true; }
            if (nativeBusy() || world.SerializationBusy || world.ArchiveWorkerRunning()) return false;
            RequireNeighbours(); CaptureCommands();
            // A native respec may change this actor's descriptor/view, and native
            // dismissal may remove it. Neither may substitute a new registration
            // under the old scope. New mounting belongs after this proof.
            var current = world.Registry.FindOwner(owner.Rider);
            Require(current == null || ReferenceEquals(current, owner), "Progression cleanup refuses a replacement rider registration.");
            current = world.Registry.FindOwner(owner.Mount);
            Require(current == null || ReferenceEquals(current, owner), "Progression cleanup refuses a replacement mount registration.");
            if (!CommandsSettled(owner.Rider) || !CommandsSettled(owner.Mount) || !observedCommands.All(CommandSettled)) return false;
            var successful = true;
            try { pair.Combat.Cancel("Selected native progression cleanup"); }
            catch (Exception exception) { Remember(exception); successful = false; }
            if (!PairCommandsSettled()) return false;
            try
            {
                if (pair.Relationship.State != RelationshipState.Unmounted || pair.Relationship.PairOwner != null ||
                    !pair.Relationship.Runtime.NoPreparedPairResidue)
                    Require(pair.Relationship.Dismount(CleanupTrigger.Exception).Succeeded,
                        "Original progression pair still owns unfinished relationship cleanup.");
            }
            catch (Exception exception) { Remember(exception); successful = false; }
            if (!successful || !CommandsSettled(owner.Rider) || !CommandsSettled(owner.Mount) || !observedCommands.All(CommandSettled) || !PairCommandsSettled() ||
                pair.Relationship.State != RelationshipState.Unmounted || pair.Relationship.PairOwner != null ||
                !pair.Relationship.Runtime.NoPreparedPairResidue || owner.RetainsObligations() || !owner.TeardownReady() ||
                controls.HasUnsettledRelationshipTransition || world.UnifiedTurn.HasUnsettledPreparation) return false;
            RequireNeighbours();
            targetSettled = true; Observe("original-progression-pair-settled"); return true;
        }
        private void RequireNeighbours()
        {
            foreach (var neighbour in neighbours)
                Require(world.Registry.IsActiveOwner(neighbour, neighbour.Rider, neighbour.Mount) &&
                    ReferenceEquals(neighbour.Boundary.PairOwner, neighbour) && neighbour.Boundary.State == RelationshipState.Mounted &&
                    neighbour.Boundary.MountedPairGeneration == neighbour.Generation,
                    "Native progression changed an independent mounted pair's registration/generation.");
        }
        private bool PairCommandsSettled() => !pair.Combat.HasActiveCommand && !pair.Combat.HasActiveGroundMovement && !pair.Combat.HasChargeOwnership &&
            !controls.HasUnsettledShellFor(pair.Relationship);
        private void CaptureCommands()
        {
            foreach (var actor in new[] { owner.Rider, owner.Mount })
                if (actor?.Commands != null) foreach (var command in actor.Commands.Raw) if (command != null) observedCommands.Add(command);
        }
        private static bool CommandsSettled(UnitEntityData actor) => actor?.Commands != null && actor.Commands.Raw.All(CommandSettled);
        private static bool CommandSettled(UnitCommand command) => command == null || command.IsFinished &&
            (!(command is UnitUseAbility ability) || ability.ExecutionProcess == null || ability.ExecutionProcess.IsEnded);
        private void Remember(Exception exception) { var error = exception.ToString(); if (!cleanupErrors.Contains(error)) cleanupErrors.Add(error); }
        private static int Id(object value) => value == null ? 0 : RuntimeHelpers.GetHashCode(value);
        private static void Require(bool value, string error) { if (!value) throw new InvalidOperationException(error); }
    }
}
