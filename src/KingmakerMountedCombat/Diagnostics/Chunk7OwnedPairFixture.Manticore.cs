using System;
using System.Linq;
using Kingmaker.Blueprints.Area;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7OwnedPairFixture
    {
        private BlueprintArea manticoreDestination;
        private bool manticorePending, manticoreAdopted, manticoreRequested;
        private bool TombFixturePreparation => request.PersistenceCase == Chunk7OwnedPairsSelection.TombFixtureCheckpoint;
        private bool ThornRiverFixturePreparation => request.PersistenceCase == Chunk7OwnedPairsSelection.ThornRiverFixtureCheckpoint;
        private string FixtureDestinationArea => TombFixturePreparation ? Chunk7OwnedPairsSelection.TombFixtureArea :
            ThornRiverFixturePreparation ? Chunk7OwnedPairsSelection.ThornRiverFixtureArea : Chunk7OwnedPairsSelection.ManticoreFixtureArea;

        internal void BeginManticorePreparation(BlueprintArea destination)
        {
            Require(bound && !released && !manticoreRequested && !manticorePending && !manticoreAdopted &&
                selection.IsOwnerSeedCheckpointSave && (request.PersistenceCase == Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint || TombFixturePreparation || ThornRiverFixturePreparation) &&
                request.PersistenceLoad.Sha256 == Chunk7OwnedPairsSelection.ManticoreFixtureSourceSha256 &&
                request.PersistenceLoad.Area == Chunk7OwnerSeedManticoreEntry.SourceArea && request.PersistenceAreaTarget == null &&
                destination != null && destination.AssetGuidThreadSafe == FixtureDestinationArea && !destination.IsCapital &&
                replayExpectedLoad == 0 && replayExpectedArea == 0 && !areaAutosavePending,
                "Unmounted Manticore preparation lacks its exact unused source-only transfer.");
            RequirePreserved(); RequireReplayFieldReady(); RequireManticoreUnmountedBoundary();
            manticoreDestination = destination; manticorePending = true; manticoreRequested = true;
        }

        internal void AdoptManticorePreparation(BlueprintArea destination)
        {
            RequireRequest(); RequireAuthority();
            Require(bound && !released && manticorePending && !manticoreAdopted &&
                selection.IsOwnerSeedCheckpointSave && (request.PersistenceCase == Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint || TombFixturePreparation || ThornRiverFixturePreparation) &&
                ReferenceEquals(destination, manticoreDestination) && destination != null &&
                destination.AssetGuidThreadSafe == FixtureDestinationArea &&
                ReferenceEquals(game.Player, admittedPlayer) && ReferenceEquals(game.State, admittedState),
                "Only the declared completed unmounted Manticore entry may adopt destination custody.");
            RequireManticoreUnmountedBoundary();
            Scope.RequireDeclaredAreaDestination(destination.AssetGuidThreadSafe, destination);
            var prior = Scope;
            Scope = new Chunk7NativeWorldScope(request, Party, destination.AssetGuidThreadSafe);
            try { RequirePreserved(); }
            catch { Scope = prior; throw; }
            manticorePending = false; manticoreAdopted = true; manticoreDestination = null;
        }

        internal void CancelManticorePreparationInOriginalWorld()
        {
            RequireRequest(); RequireAuthority();
            Require(bound && !released && manticoreRequested && manticorePending && !manticoreAdopted &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == Chunk7OwnerSeedManticoreEntry.SourceArea,
                "A failed Manticore dispatch can release transfer custody only in its exact captured source world.");
            Scope.RequireCurrent(); RequirePreserved(); RequireManticoreUnmountedBoundary();
            manticorePending = false; manticoreDestination = null;
        }

        private void RequireManticoreUnmountedBoundary() => Require(world.Registry.Owners.Length == 0 &&
            world.Registry.ActiveCount == 0 && !world.UnifiedTurn.HasUnsettledPreparation && !controls.HasUnsettledRelationshipTransition &&
            world.Pairs.All(pair => pair.Relationship.State == RelationshipState.Unmounted && pair.Relationship.PairOwner == null &&
                pair.Relationship.Runtime.NoPreparedPairResidue && !pair.Combat.HasRetainedPairCommands) &&
            persistence.AreaSuspensionCount == 0 && persistence.AreaResumeCount == 0 && !persistence.AreaTransitionPending &&
            persistence.FailedAreaPairCount == 0 && persistence.NativeWorldDisposalCount == 0 &&
            persistence.SemanticRestoreCount == 4 && persistence.PresentationRestoreCount == 2 && persistence.ArchivePostLoadObservedCount == 4 &&
            persistence.SnapshotCount == 0 && controls.NativeCastRequestCount == 2 &&
            !persistence.HasActiveSaveScope && !persistence.HasSaveAdmissionFence && !NativePersistenceIsolation.HasPendingWrites,
            "Unmounted fixture entry acquired mounted restoration, native input or save obligations.");
    }
}
