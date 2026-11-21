using System;
using System.Collections.Generic;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Logging;

namespace KingmakerMountedCombat.Integration
{
    // The narrow native adapter for CityCompanionRosterProjection: reads the exact owner/pet links of one native
    // UIUtility.GetGroup result and returns the projected list. It reads state only; the same persistent pet instance
    // the owner links to is listed, so selection, inventory and the character sheet bind that exact unit.
    internal static class CityCompanionRosterAdapter
    {
        // Kingmaker 2.1.7b UIUtility.GetGroup(bool withRemote, bool withPet), pinned in Test-AssemblyContracts.ps1.
        internal const int NativeGetGroupToken = 0x06004281;

        private static bool failureReported;

        internal static long ProjectedPetCount { get; private set; }

        // modEnabled: Mounted Combat's services are enabled (UMM toggle on). A disabled mod returns the native list.
        internal static List<UnitEntityData> Project(List<UnitEntityData> nativeGroup, bool withRemote, bool withPet,
            bool modEnabled, IModLogger logger)
        {
            if (nativeGroup == null || !modEnabled || !withRemote || !withPet) return nativeGroup;
            try
            {
                var game = Game.Instance;
                var player = game?.Player;
                if (player == null) return nativeGroup;
                var characters = player.AllCharacters;
                var projected = CityCompanionRosterProjection.ProjectRoster(nativeGroup, modEnabled,
                    game.CurrentlyLoadedArea?.IsCapital == true, withRemote, withPet, owner =>
                {
                    var pet = owner.Descriptor?.Pet;
                    var link = new CityCompanionRosterProjection.PetLink
                    {
                        OwnerIsPet = owner.Descriptor == null || owner.Descriptor.IsPet,
                        OwnerDetached = owner.IsDetached,
                        OwnerExCompanion = player.ExCompanions.Exists(reference => ReferenceEquals(reference.Value, owner)),
                        HasPet = pet != null,
                        PetIsPet = pet?.Descriptor != null && pet.Descriptor.IsPet,
                        PetMasterIsOwner = pet?.Descriptor != null && ReferenceEquals(pet.Descriptor.Master.Value, owner),
                        PetIsSummon = pet != null && pet.Get<UnitPartSummonedMonster>() != null,
                        PetIsPlayerCharacter = pet != null && characters.Contains(pet)
                    };
                    return CityCompanionRosterProjection.IsEligiblePet(link) ? pet : null;
                });
                ProjectedPetCount += projected.Count - nativeGroup.Count;
                return projected;
            }
            catch (Exception error)
            {
                // The native roster is always the fallback; one line per session, never per frame.
                if (!failureReported)
                {
                    failureReported = true;
                    logger?.Error("City companion roster projection failed; the native roster is shown unchanged: " + error.Message);
                }
                return nativeGroup;
            }
        }
    }
}
