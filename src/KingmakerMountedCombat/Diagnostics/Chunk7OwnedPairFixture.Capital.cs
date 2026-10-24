using Kingmaker.Blueprints.Area;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7OwnedPairFixture
    {
        // Matrix row 58: the one declared transfer into the reviewed capital. The inherited roster, pets,
        // facts, views and source receipt stay exactly preserved; only the world scope is re-pinned, after
        // the product's restore attempt has completed with exactly one restored and one retired pair.
        private bool capitalPending, capitalAdopted;
        // The companion pair native capital mode took out of the scene (see Chunk7OwnedPairFixture.CapitalRoster.cs).
        private Kingmaker.EntitySystem.Entities.UnitEntityData[] capitalOutOfScene;
        private BlueprintArea capitalDestination;
        internal bool CapitalPending => capitalPending;
        internal void BeginCapitalArrival(BlueprintArea destination)
        {
            Require(bound && !released && selection.Cold && selection.IsCapitalArrivalProducer(request.PersistenceCase) &&
                request.PersistenceAreaTarget != null && request.PersistenceAreaTarget.AutoSaveMode == "None" &&
                request.PersistenceAreaTarget.EnterPoint == Chunk7OwnedPairsSelection.CapitalArrivalEntry &&
                request.PersistenceAreaTarget.Area == Chunk7OwnedPairsSelection.CapitalArrivalArea &&
                !capitalPending && !capitalAdopted && replayExpectedLoad == 0 && replayExpectedArea == 0 && destination != null &&
                destination.AssetGuidThreadSafe == Chunk7OwnedPairsSelection.CapitalArrivalArea && destination.IsCapital &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == request.PersistenceLoad.Area && !ReferenceEquals(destination, game.CurrentlyLoadedArea),
                "Inherited capital transfer is not the exact declared capital destination from the loaded Manual area.");
            RequirePreserved(); RequireReplayFieldReady();
            capitalDestination = destination; capitalPending = true;
        }
        internal void AdoptCapitalArrival(BlueprintArea destination)
        {
            RequireRequest(); RequireAuthority();
            Require(bound && !released && selection.Cold && selection.IsCapitalArrivalProducer(request.PersistenceCase) &&
                capitalPending && !capitalAdopted && replayExpectedLoad == 0 && replayExpectedArea == 0 &&
                ReferenceEquals(destination, capitalDestination) && destination.IsCapital && ReferenceEquals(game.Player, admittedPlayer) &&
                persistence.AreaSuspensionCount == 1 && persistence.AreaResumeCount == 1 && persistence.AreaRetiredPairCount == 1 &&
                persistence.FailedAreaPairCount == 0 && persistence.NativeWorldDisposalCount == 0 && persistence.SemanticRestoreCount == 4 &&
                persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4,
                "Inherited capital custody cannot adopt before exactly one restored and one retired pair.");
            Scope.RequireDeclaredAreaDestination(destination.AssetGuidThreadSafe, destination);
            var prior = Scope;
            var candidate = new Chunk7NativeWorldScope(request, Party, destination.AssetGuidThreadSafe);
            // Read-only validation interval: the roster, pets, facts and views must hold, except that native
            // capital mode may take exactly the retired companion pair (inherited pair B) out of the scene.
            Scope = candidate; capitalOutOfScene = new[] { Actors[2], Actors[3] };
            try { RequirePreserved(); }
            catch { Scope = prior; capitalOutOfScene = null; throw; }
            capitalPending = false; capitalAdopted = true; capitalDestination = null;
        }
    }
}
