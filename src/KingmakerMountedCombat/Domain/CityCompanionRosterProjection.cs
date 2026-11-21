using System;
using System.Collections.Generic;

namespace KingmakerMountedCombat.Domain
{
    // City animal-companion inspection. In a capital the native full-screen Inventory, Character and Spellbook roster
    // (GroupController -> UIUtility.GetGroup(withRemote: true, withPet: true)) lists every party and reserve (remote)
    // companion, in or out of the game, but takes pets only from Player.ControllableCharacters, which requires the pet
    // to be in the game. CapitalCompanionLogic takes custom companions, and companions without a capital spawn place,
    // out of the game, and their pets with them, so those owners appear while their animal companions do not.
    //
    // This projection adds exactly those owners' own pets, after their owner as native orders pets, to the list one
    // GetGroup call returns. It never changes the party, ownership, IsInGame, formation, scenes or selection.
    public static class CityCompanionRosterProjection
    {
        // The facts one roster member and its linked pet present. Collected by the adapter from native state.
        public sealed class PetLink
        {
            // The roster member is itself a pet (pets are never owners here).
            public bool OwnerIsPet;
            // Native ControllableCharacters already refuses pets of detached owners and of ex-companions.
            public bool OwnerDetached;
            public bool OwnerExCompanion;
            public bool HasPet;
            // The linked unit is a native pet whose Master is exactly this owner (reciprocal link).
            public bool PetIsPet;
            public bool PetMasterIsOwner;
            // A temporary summon is never an animal companion.
            public bool PetIsSummon;
            // The linked unit is one of the player's own cross-scene characters (Player.AllCharacters): never an
            // unrelated animal, a scene creature or a visual stand-in.
            public bool PetIsPlayerCharacter;
        }

        public static bool IsEligiblePet(PetLink link) =>
            link != null && !link.OwnerIsPet && !link.OwnerDetached && !link.OwnerExCompanion && link.HasPet &&
            link.PetIsPet && link.PetMasterIsOwner && !link.PetIsSummon && link.PetIsPlayerCharacter;

        // Only the capital's full-screen pet roster is projected: the Vendor roster (withRemote without pets), the field
        // roster (pets without remote) and every other caller keep the native list unchanged.
        // A disabled Mounted Combat (UMM toggle off; its patches stay installed until disposal) leaves every roster native.
        public static bool AppliesTo(bool modEnabled, bool areaIsCapital, bool withRemote, bool withPet) =>
            modEnabled && areaIsCapital && withRemote && withPet;

        // One native GetGroup result: the same list object when the projection does not apply, otherwise a new list.
        public static List<T> ProjectRoster<T>(List<T> nativeGroup, bool modEnabled, bool areaIsCapital, bool withRemote,
            bool withPet, Func<T, T> eligiblePetOf) where T : class =>
            nativeGroup == null || !AppliesTo(modEnabled, areaIsCapital, withRemote, withPet)
                ? nativeGroup : Project(nativeGroup, eligiblePetOf);

        // nativeGroup: the list one native call returned, in its order. eligiblePetOf: the owner's own pet when
        // IsEligiblePet admits it, otherwise null. Returns a new list; a pet already listed is never repeated.
        public static List<T> Project<T>(IList<T> nativeGroup, Func<T, T> eligiblePetOf) where T : class
        {
            if (nativeGroup == null) return null;
            var result = new List<T>(nativeGroup);
            if (eligiblePetOf == null) return result;
            foreach (var owner in nativeGroup)
            {
                if (owner == null) continue;
                var pet = eligiblePetOf(owner);
                if (pet == null || ReferenceEquals(pet, owner) || result.Contains(pet)) continue;
                result.Insert(result.IndexOf(owner) + 1, pet);
            }
            return result;
        }
    }
}
