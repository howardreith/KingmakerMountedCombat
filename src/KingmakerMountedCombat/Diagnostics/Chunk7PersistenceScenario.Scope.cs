using System;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7PersistenceScenario
    {
        private Chunk7NativeWorldScope nativeScope;
        private object beforeLoadGame;

        private void RequireInitialLoadGame()
        {
            Require(beforeLoadGame != null && ReferenceEquals(beforeLoadGame, Game.Instance),
                "Native persistence load/setting lease belongs to a replaced Game.");
        }

        private void RequireNativeWorld()
        {
            Require(nativeScope != null, "No admitted native world is owned by this persistence scenario.");
            nativeScope.RequireCurrent();
        }

        private void TrackNativeActors()
        {
            RequireNativeWorld();
            foreach (var actor in (actors ?? new UnitEntityData[0]).Concat(new[] { firstRider, firstMount, secondRider, secondMount }))
                nativeScope.Track(actor);
        }

        private void RequireCleanupWorld()
        {
            RequireRepeatedCleanupWorld();
            if (nativeScope == null)
            {
                // Before native loading begins, only this exact menu Game owns
                // the temporary settings/observer leases. A partially loaded or
                // substituted world is not silently adopted to release them.
                Require(!captured && ReferenceEquals(beforeLoadGame, Game.Instance) &&
                    Game.Instance != null && Game.Instance.CurrentlyLoadedArea == null,
                    "Unadmitted or replaced loaded world retains persistence cleanup custody.");
                return;
            }
            if (AreaAutosaveCase && !Cold && stage == 30 && !areaScopeAdopted)
            {
                // A failed dispatch can still be in the exact source world.
                // Otherwise only the declared, fully restored native destination
                // may adopt a new area-state identity for cleanup.
                if (Game.Instance?.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.PersistenceAreaTarget?.Area)
                    AdoptDeclaredAreaWorld();
            }
            if (ManticoreFixtureCase && manticoreDispatched && !manticoreScopeAdopted &&
                Game.Instance?.CurrentlyLoadedArea?.AssetGuidThreadSafe == Chunk7OwnedPairsSelection.ManticoreFixtureArea)
                AdoptManticoreFixtureWorld();
            else if (ManticoreFixtureCase && manticoreDispatched && !manticoreScopeAdopted &&
                Game.Instance?.CurrentlyLoadedArea?.AssetGuidThreadSafe == Chunk7OwnerSeedManticoreEntry.SourceArea)
            {
                inherited.CancelManticorePreparationInOriginalWorld();
                manticoreDispatched = false;
            }
            RequireNativeWorld();
        }
    }
}
