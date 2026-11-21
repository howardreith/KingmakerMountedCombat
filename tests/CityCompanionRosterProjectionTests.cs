using System.Collections.Generic;
using System.Linq;
using KingmakerMountedCombat.Domain;

namespace KingmakerMountedCombat.Tests
{
    // City companion inspection: the capital's full-screen roster projection. Units are labeled test doubles; the
    // native list is what UIUtility.GetGroup(withRemote, withPet) returned for them.
    internal static class CityCompanionRosterProjectionTests
    {
        internal static void Register(TestRunner runner)
        {
            runner.Run("city roster adds a reserve owner's own animal companion right after its owner", AddsReserveOwnersPet);
            runner.Run("city roster lists multiple pets once each, in owner order, and never repeats a native pet", MultiplePetsNoDuplicates);
            runner.Run("city roster excludes summons, non-reciprocal links, foreign animals and ineligible owners", Exclusions);
            runner.Run("city roster projection applies only to the capital's full-screen pet roster", AppliesOnlyToCapitalPetRoster);
            runner.Run("city roster projection is repeatable and leaves the native list untouched", RepeatableAndPure);
        }

        private sealed class Unit
        {
            internal string Name;
            internal Unit Pet;
            internal Unit Master;
            internal bool Summon, Detached, ExCompanion, Foreign;
            public override string ToString() => Name;
        }

        private static Unit Owner(string name) => new Unit { Name = name };

        private static Unit WithPet(Unit owner, string petName)
        {
            var pet = new Unit { Name = petName, Master = owner };
            owner.Pet = pet;
            return pet;
        }

        // Mirrors CityCompanionRosterAdapter's reading of native state.
        private static Unit EligiblePet(Unit owner)
        {
            var pet = owner.Pet;
            var link = new CityCompanionRosterProjection.PetLink
            {
                OwnerIsPet = owner.Master != null,
                OwnerDetached = owner.Detached,
                OwnerExCompanion = owner.ExCompanion,
                HasPet = pet != null,
                PetIsPet = pet != null && pet.Master != null,
                PetMasterIsOwner = pet != null && ReferenceEquals(pet.Master, owner),
                PetIsSummon = pet != null && pet.Summon,
                PetIsPlayerCharacter = pet != null && !pet.Foreign
            };
            return CityCompanionRosterProjection.IsEligiblePet(link) ? pet : null;
        }

        private static string Names(IEnumerable<Unit> units) => string.Join(",", units.Select(u => u.Name).ToArray());

        private static void AddsReserveOwnersPet()
        {
            var main = Owner("Main"); var mainHorse = WithPet(main, "MainHorse");
            var mercenary = Owner("Mercenary"); WithPet(mercenary, "MercHorse");
            var valerie = Owner("Valerie");
            // Native: the main pet is in the game (listed after Main); the mercenary is a reserve owner out of the game,
            // listed, but its pet is not because native takes pets only from ControllableCharacters.
            var native = new List<Unit> { main, mainHorse, mercenary, valerie };
            var projected = CityCompanionRosterProjection.Project(native, EligiblePet);
            TestRunner.Equal("Main,MainHorse,Mercenary,MercHorse,Valerie", Names(projected),
                "The reserve owner's pet was not listed right after its owner, or an owner without a pet gained one.");
            TestRunner.True(ReferenceEquals(projected[3], mercenary.Pet), "The listed pet is not the owner's exact persistent instance.");
        }

        private static void MultiplePetsNoDuplicates()
        {
            var a = Owner("A"); WithPet(a, "APet");
            var b = Owner("B"); var bPet = WithPet(b, "BPet");
            var c = Owner("C"); WithPet(c, "CPet");
            var native = new List<Unit> { a, b, bPet, c };
            var projected = CityCompanionRosterProjection.Project(native, EligiblePet);
            TestRunner.Equal("A,APet,B,BPet,C,CPet", Names(projected), "Multiple pets were not each listed once after their owners.");
            TestRunner.Equal(projected.Count, projected.Distinct().Count(), "A portrait was duplicated.");
        }

        private static void Exclusions()
        {
            var summoner = Owner("Summoner"); WithPet(summoner, "Summon").Summon = true;
            var liar = Owner("Liar"); liar.Pet = new Unit { Name = "Stray", Master = Owner("Someone") };
            var foreign = Owner("Foreign"); WithPet(foreign, "SceneWolf").Foreign = true;
            var gone = Owner("Ex"); gone.ExCompanion = true; WithPet(gone, "ExPet");
            var away = Owner("Detached"); away.Detached = true; WithPet(away, "DetachedPet");
            var notListed = Owner("NotInRoster"); WithPet(notListed, "UnlistedPet");
            var lonely = Owner("NoPet");
            var native = new List<Unit> { summoner, liar, foreign, gone, away, lonely };
            var projected = CityCompanionRosterProjection.Project(native, EligiblePet);
            TestRunner.Equal(Names(native), Names(projected),
                "A summon, a non-reciprocal link, a foreign animal, an ineligible owner's pet or an unlisted owner's pet was added.");
        }

        private static void AppliesOnlyToCapitalPetRoster()
        {
            TestRunner.True(CityCompanionRosterProjection.AppliesTo(true, true, true), "The capital's Inventory/Character roster was not projected.");
            TestRunner.True(!CityCompanionRosterProjection.AppliesTo(true, true, false), "The capital Vendor roster gained pets.");
            TestRunner.True(!CityCompanionRosterProjection.AppliesTo(false, false, true), "The field roster was changed.");
            TestRunner.True(!CityCompanionRosterProjection.AppliesTo(false, true, true), "A non-capital caller was projected.");
            TestRunner.True(!CityCompanionRosterProjection.AppliesTo(true, false, false), "The plain party roster was changed.");
        }

        private static void RepeatableAndPure()
        {
            var main = Owner("Main"); var merc = Owner("Merc"); WithPet(merc, "MercPet");
            var native = new List<Unit> { main, merc };
            var first = CityCompanionRosterProjection.Project(native, EligiblePet);
            var second = CityCompanionRosterProjection.Project(native, EligiblePet);
            TestRunner.Equal(Names(first), Names(second), "Repeated panel opening produced a different roster.");
            TestRunner.Equal("Main,Merc", Names(native), "The native list was modified in place.");
            // Reprojecting an already projected list (another caller layering on it) adds nothing.
            TestRunner.Equal(Names(first), Names(CityCompanionRosterProjection.Project(first, EligiblePet)), "A projected pet was repeated.");
            TestRunner.True(CityCompanionRosterProjection.Project<Unit>(null, EligiblePet) == null, "A missing native list was invented.");
        }
    }
}
