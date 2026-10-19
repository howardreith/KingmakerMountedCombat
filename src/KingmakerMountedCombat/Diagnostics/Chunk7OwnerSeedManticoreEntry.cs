using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.GameModes;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // Disclosed fixture assistance, never an explored-location or ordinary-travel
    // claim. Reading the existing row cannot add or change GlobalMap state.
    internal static class Chunk7OwnerSeedManticoreEntry
    {
        internal const string SourceArea = "b10e86516b6437d45a629931c30936fe";
        internal const string Location = "3cf6c925d3fca1943b538db776f90b20";
        internal const string Entry = "dfbc24234b2f33f4f888bb6f5f838c6b";

        internal static BlueprintAreaEnterPoint Resolve(Game game, RuntimeRequest request, out JObject observation)
        {
            observation = null;
            Require(request?.OwnedPairs?.IsOwnerSeedCheckpointSave == true &&
                request.PersistenceCase == Chunk7OwnedPairsSelection.ManticoreFixtureCheckpoint &&
                request.PersistenceLoad?.Sha256 == Chunk7OwnedPairsSelection.ManticoreFixtureSourceSha256 &&
                request.PersistenceLoad.Area == SourceArea && request.PersistenceAreaTarget == null &&
                request.PersistenceReplayTargets == null && game?.Player != null &&
                game.Player.GameId == request.PersistenceLoad.GameId && !game.Player.IsInCombat &&
                game.CurrentMode == GameModeType.Default && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == SourceArea,
                "Manticore fixture preparation requires only its exact accepted source and closed native route.");
            var map = game.Player.GlobalMap;
            var location = ResourcesLibrary.TryGetBlueprint<BlueprintLocation>(Location);
            LocationData data;
            Require(map?.Locations != null && location != null && location.AssetGuidThreadSafe == Location &&
                map.Locations.TryGetValue(location, out data) && data != null && ReferenceEquals(data.Blueprint, location) &&
                data.IsSeen && data.IsRevealed && !data.IsClosed && !data.IsExplored,
                "Manticore fixture preparation requires its existing revealed/open, still unexplored saved location.");
            data = map.Locations[location];
            var count = map.Locations.Count;
            var resolved = data.AreaEntrance;
            var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(Entry);
            Require(entry != null && ReferenceEquals(resolved, entry) && entry.AssetGuidThreadSafe == Entry &&
                entry.Area != null && entry.Area.AssetGuidThreadSafe == Chunk7OwnedPairsSelection.ManticoreFixtureArea &&
                !entry.Area.IsCapital && !ReferenceEquals(entry.Area, game.CurrentlyLoadedArea) && map.Locations.Count == count,
                "Manticore fixture preparation refused a different actual entrance or a capital destination.");
            observation = new JObject {
                ["contract"] = "owner-seed-manticore-entry-v1", ["sourceArea"] = SourceArea,
                ["sourceSha256"] = request.PersistenceLoad.Sha256,
                ["locationGuid"] = location.AssetGuidThreadSafe, ["entryGuid"] = resolved.AssetGuidThreadSafe,
                ["destinationArea"] = entry.Area.AssetGuidThreadSafe, ["destinationIsCapital"] = entry.Area.IsCapital,
                ["seen"] = data.IsSeen, ["revealed"] = data.IsRevealed, ["closed"] = data.IsClosed, ["explored"] = data.IsExplored,
                ["locationCountBefore"] = count, ["locationCountAfter"] = map.Locations.Count,
                ["existingLocationOnly"] = true, ["actualEntranceExact"] = true,
                ["navigationAssisted"] = true, ["ordinaryTravelProof"] = false, ["nativeMode"] = "None",
                ["transferRequests"] = 0, ["saveRequests"] = 0 };
            return entry;
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
    }
}
