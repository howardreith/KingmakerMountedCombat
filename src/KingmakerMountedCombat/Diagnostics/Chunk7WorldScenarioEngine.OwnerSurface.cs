using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;

namespace KingmakerMountedCombat.Diagnostics
{
    // Sprint 2 (MOVE-01 on the owner's surface): the pre-owned single-pair speed mode. A disposable run copy of
    // the owner's own save is loaded through the Missing-metadata path (no KMC archive member, nothing restored);
    // the one pre-owned pair (the owner's rider and the KMC Horse it already owns) is bound exactly, nothing is
    // built, granted, levelled or acquired, no second pair exists, and the four aligned speed legs run on that
    // pair: mounted with the whole party, mounted alone, the Horse alone, the rider on foot. The only inputs are
    // the ordinary native Mount and Dismount controls and ordinary ground clicks; no save is written.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string OwnerSurfaceContract = "owner-surface-pre-owned-pair-speed-v1";
        internal const string OwnerSurfaceMountRow = "C7-world-owner-surface-mount";
        internal const string OwnerSurfaceLegsRow = "C7-world-owner-surface-legs";
        private const double OwnerSurfaceTotalSeconds = 360, OwnerSurfaceStepSeconds = 60;
        private bool OwnerSurfaceSpeed => request.OwnedPairs?.IsOwnerSurfaceSpeed == true;
        // Finishing mission 2026-10-31: the explicit opt-in runs the single-pair turn-based door stage on the owner's pre-owned
        // pair (shared combat-usable door selection first, no speed legs, dismount, cleanup) instead of the speed legs.
        private bool OwnerSurfaceDoorSelected => OwnerSurfaceSpeed && request.OwnedPairs?.OwnerSurfaceDoorSelected == true;
        private JObject ownerSurface;
        private UnitEntityData ownerSurfaceOther;

        private void BindOwnerSurface(Game game)
        {
            var selection = request.OwnedPairs; var input = request.PersistenceLoad;
            RequireOwnerSurfaceAuthority();
            Require(horses.State == HorseCompanionBlueprintState.Registered && horses.HorseFeature != null && horses.LevelRank != null && horses.HorseUnit != null,
                "Exact registered ordinary Horse contracts are unavailable.");
            // The owner's save carries no KMC archive member: it loads through the supported missing-metadata path and
            // restores no mounted state (the pair starts unmounted exactly as the native load left it).
            Require(game.Player.MainCharacter.Value?.CharacterName == input.GameName && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == input.Area &&
                Chunk7OwnerSeedObservation.AdmittedSeedMetadata(persistence, false, input.GameId, input.Area),
                "The owner's save must load through the supported missing-metadata path (it proves no mounted restoration).");
            originalParty = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(originalParty.Select(x => x.UniqueId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(selection.PartyIds.OrderBy(x => x, StringComparer.Ordinal)),
                "Loaded native field roster differs from the selected owner party. No substitute actor is selected.");
            var rider = Exact(selection.RiderAId); var pet = Exact(selection.MountAId);
            var feature = rider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(ReferenceEquals(rider.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, rider) && pet.Blueprint == horses.HorseUnit &&
                feature != null && feature.Source != null && rider.Descriptor.GetFact(horses.LevelRank) != null && rider.IsDirectlyControllable &&
                !rider.Descriptor.IsPet && pet.Descriptor.IsPet && rider.GetActivePolymorph() == null,
                "The owner's pinned pre-owned reciprocal KMC Horse pair is unavailable in the loaded save.");
            string rigError;
            Require(MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, SupportedMountedProfiles.Horse.RiderPoseProfile, out rigError),
                "Owner rider rig refused: " + rigError);
            var profile = SupportedMountedProfiles.Resolve(rider, pet);
            Require(profile != null && profile.Id == SupportedMountedProfiles.Horse.Id && SupportedMountedProfiles.IsSupportedPair(rider, pet),
                "The owner's pair does not resolve the supported Horse profile.");
            // The speed legs measure the second-pair slots; the owner's pair is the only pair.
            firstRider = null; firstMount = null; secondRider = rider; secondMount = pet;
            // Selection changes after an input go to another controllable, non-pet party member when one exists.
            ownerSurfaceOther = originalParty.FirstOrDefault(u => !ReferenceEquals(u, rider) && !ReferenceEquals(u, pet) && u.IsDirectlyControllable && !u.Descriptor.IsPet) ?? rider;
            evidence["partyBefore"] = new JArray(originalParty.Select(CaptureRig));
            var facts = RiderFacts(rider);
            ownerSurface = new JObject { ["contract"] = OwnerSurfaceContract, ["seedId"] = selection.SeedId, ["seedSha256"] = selection.SeedSha256,
                ["riderId"] = rider.UniqueId, ["horseId"] = pet.UniqueId, ["profileId"] = profile.Id,
                ["horseBlueprint"] = pet.Blueprint.AssetGuid, ["horseFeatureSourceGuid"] = feature.Source.AssetGuid,
                ["preOwnedPair"] = PreOwnedRow(facts), ["fixtureSetupOnly"] = false, ["acquisitionProof"] = false, ["producerAncestry"] = false,
                ["nativeLoadVerifiedByHost"] = true, ["loadedArchiveKind"] = persistence.LoadedArchiveKind?.ToString(),
                ["loadedArchiveCanRestore"] = persistence.LoadedArchiveCanRestore, ["mutations"] = "none: no build, grant, level, XP, item, money or save",
                ["secondPair"] = false, ["selectionChangeActorId"] = ownerSurfaceOther.UniqueId, ["doorStage"] = selection.OwnerSurfaceDoorSelected,
                ["area"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe, ["gameName"] = input.GameName, ["gameId"] = game.Player.GameId,
                ["partyIds"] = new JArray(originalParty.Select(u => u.UniqueId)) };
            evidence["ownerSurface"] = ownerSurface;
        }

        private void RequireOwnerSurfaceAuthority()
        {
            var input = request.PersistenceLoad;
            Require(request.OwnedPairs?.IsOwnerSurfaceSpeed == true && request.Validate().Count == 0 && input != null &&
                fixtureAuthorization != null && fixtureAuthority != null && fixtureLoader != null && fixtureAuthorization.IsActive &&
                fixtureAuthorization.IsExactPersistenceScope(fixtureAuthority) && NativePersistenceIsolation.IsExactAuthority(fixtureAuthority) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, false) && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == 0 && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                fixtureAuthorization.UnauthorizedLoadCount == 0 && !NativePersistenceIsolation.NativeSlotRotationEnabled &&
                Game.Instance?.SaveManager != null && Game.Instance.SaveManager.SavePath == fixtureAuthority.Root &&
                fixtureLoader.State == WorkingFixtureLoadState.LoadedAndVerified &&
                fixtureLoader.WorkingPath == System.IO.Path.Combine(fixtureAuthority.Root, input.FileName) && fixtureLoader.Descriptor != null &&
                fixtureLoader.Descriptor.FileName == input.FileName && fixtureLoader.Descriptor.GameId == input.GameId &&
                fixtureLoader.Descriptor.Area?.AssetGuidThreadSafe == input.Area,
                "Owner-surface speed lost its exact one-load, zero-write isolated authority.");
        }

        private void RequireOwnerSurfacePair()
        {
            Require(ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) &&
                secondMount.IsInState && secondRider.IsInState && originalParty.Contains(secondMount) && world.Registry.ActiveCount <= 1,
                "The owner's pre-owned pair lost its reciprocal native custody.");
            RequireOwnerSurfaceAuthority();
        }

        private bool OwnerSurfacePairPreserved() => secondRider != null && secondMount != null &&
            ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) &&
            secondMount.IsInState && secondRider.IsInState && Game.Instance.State.Units.Contains(secondMount);

        private void BeginOwnerSurface()
        {
            Require(OwnerSurfaceSpeed && ownerSurface != null && secondRider != null && secondMount != null, "Owner-surface speed mode was not bound.");
            Next(200);
        }

        private void UpdateOwnerSurface()
        {
            // The turn-based door stage (step 210) carries its own native encounter and the probe's 240 s bound.
            Require(clock.Elapsed.TotalSeconds <= OwnerSurfaceTotalSeconds && clock.Elapsed.TotalSeconds - stepStarted <= (step == 210 ? 240 : OwnerSurfaceStepSeconds),
                "Bounded owner-surface speed deadline at step " + step + ".");
            Require(step == 210 || !Game.Instance.Player.IsInCombat, "Owner-surface speed unexpectedly entered combat.");
            if (shell != null && controls.HasExactRelationshipProcessBinding(shell)) observedExactProcess = true;
            switch (step)
            {
                case 200:
                    if (CombatController.IsInTurnBasedCombat() || !Actors().All(Idle)) return;
                    RequireOwnerSurfacePair();
                    snapshots.Add(CaptureWorld("owner-surface-start"));
                    // Review 2026-10-31: one bounded read-only door inventory of the owner's surface area (combat-usability
                    // flag beside eligibility); nothing is acquired, closed or clicked.
                    evidence["doorInventory"] = Chunk7NativeDoorProbe.CaptureAreaDoorInventory(new[] { secondMount }, "owner-surface-start");
                    Input(secondRider, secondMount, controls.MountAbility, "mount-owner-pair", ownerSurfaceOther); Next(201); break;
                case 201:
                    if (!Mounted(secondRider, secondMount) || !InputSettled()) return;
                    secondOwner = world.Registry.FindActive(secondRider).Id; FinishInput();
                    Require(world.Registry.ActiveCount == 1, "The owner's single pair produced more than one active owner.");
                    Pass(OwnerSurfaceMountRow); snapshots.Add(CaptureWorld("owner-surface-mounted"));
                    if (OwnerSurfaceDoorSelected) { BeginOwnerSurfaceDoor(); Next(210); break; }
                    BeginSpeedLeg("mounted-party", SpeedLegSelection.Party); Next(202); break;
                case 210:
                    RequireOwnerSurfaceMounted(); if (!PollTurnBasedDoor()) return;
                    Pass(TurnBasedDoorRow); snapshots.Add(CaptureWorld("turn-based-door-complete"));
                    Input(secondRider, secondRider, controls.DismountAbility, "dismount-owner-pair", ownerSurfaceOther); Next(211); break;
                case 211:
                    if (Mounted(secondRider, secondMount) || !InputSettled()) return;
                    FinishInput(); RequireOwnerSurfacePair();
                    snapshots.Add(CaptureWorld("owner-surface-dismounted"));
                    ownerSurface["legsCompleted"] = 0; ownerSurface["legsApplicable"] = 0;
                    snapshots.Add(CaptureWorld("owner-surface-door-complete"));
                    BeginCleanup(); break;
                case 202:
                    RequireOwnerSurfaceMounted(); if (!PollSpeedLeg()) return;
                    BeginSpeedLeg("mounted-solo", SpeedLegSelection.SecondRiderAlone); Next(203); break;
                case 203:
                    RequireOwnerSurfaceMounted(); if (!PollSpeedLeg()) return;
                    Input(secondRider, secondRider, controls.DismountAbility, "dismount-owner-pair", ownerSurfaceOther); Next(204); break;
                case 204:
                    if (Mounted(secondRider, secondMount) || !InputSettled()) return;
                    FinishInput(); RequireOwnerSurfacePair();
                    snapshots.Add(CaptureWorld("owner-surface-dismounted"));
                    BeginSpeedLeg("horse-solo", SpeedLegSelection.SecondMountAlone); Next(205); break;
                case 205:
                    if (!PollSpeedLeg()) return;
                    BeginSpeedLeg("rider-solo", SpeedLegSelection.SecondRiderAlone); Next(206); break;
                case 206:
                    if (!PollSpeedLeg()) return;
                    RequireOwnerSurfacePair();
                    ownerSurface["legsCompleted"] = speedLegs == null ? 0 : speedLegs.Count;
                    ownerSurface["legsApplicable"] = speedLegs == null ? 0 : speedLegs.Count(leg => (bool)leg["applicable"]);
                    // v5: every leg ran (a failed leg continues the sequence for diagnosis); the row passes only when all were accepted.
                    RequireSpeedLegsAccepted();
                    Pass(OwnerSurfaceLegsRow); snapshots.Add(CaptureWorld("owner-surface-legs-complete"));
                    BeginCleanup(); break;
                default: throw new InvalidOperationException("Unknown owner-surface speed step " + step + ".");
            }
        }

        // The single-pair turn-based door stage: the shared selection requires a combat-usable qualifying door (NotInCombat
        // false) within the scan radius of the Horse BEFORE any preparation or encounter; an absence fails the row here with
        // the read-only inventory. The probe then runs with the one pre-owned pair; the selection after inputs goes to the
        // declared other party member.
        private void BeginOwnerSurfaceDoor()
        {
            Require(turnBasedDoor == null && OwnerSurfaceDoorSelected, "The owner-surface door stage runs once on the explicit opt-in.");
            var mounts = new[] { secondMount };
            var door = Chunk7NativeDoorProbe.FindQualifyingDoor(mounts, true, out var scanned);
            if (door == null)
            {
                evidence["door"] = Chunk7NativeDoorProbe.NotApplicable(mounts, true);
                Require(false, "No combat-usable qualifying door (StandardDoor.NotInCombat false) lies within the 28 m scan radius of the owner's Horse on this surface: " +
                    "the turn-based door control is not possible here; the read-only door inventory is recorded under door.rows (no preparation, no encounter).");
            }
            var facts = Chunk7NativeDoorProbe.DoorFacts.Capture(door, mounts);
            evidence["door"] = new JObject { ["applicable"] = true, ["requiredCombatUsable"] = true, ["candidatesScanned"] = scanned, ["radius"] = Chunk7NativeDoorProbe.ScanRadius,
                ["mounts"] = new JArray(mounts.Select(m => new JObject { ["id"] = m.UniqueId, ["position"] = Point(m.Position) })), ["selected"] = facts.Row(door) };
            turnBasedDoor = new Chunk7TurnBasedDoorProbe(world, nativeScope, request, logger, SelectNativeMode, door, secondRider, secondMount, null, null, originalParty)
                { SelectionActor = ownerSurfaceOther };
            evidence["turnBasedDoor"] = turnBasedDoor.Evidence;
            turnBasedDoor.Begin();
        }

        private void RequireOwnerSurfaceMounted()
        {
            Require(Mounted(secondRider, secondMount) && world.Registry.ActiveCount == 1 && world.Registry.FindActive(secondRider).Id == secondOwner,
                "Selection or command delivery changed the owner pair's exact actor/owner binding.");
        }
    }
}
