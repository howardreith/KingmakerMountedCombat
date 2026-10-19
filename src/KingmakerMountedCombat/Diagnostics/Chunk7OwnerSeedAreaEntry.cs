using System;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.Globalmap.Blueprints;
using Kingmaker.Globalmap.State;
using Kingmaker.GameModes;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    // This reads an existing campaign location. It does not reveal, move, enter,
    // mutate map state or choose an authored exit. The surrounding selected
    // scenario owns one disclosed controlled native cross-entry after this gate.
    internal static class Chunk7OwnerSeedAreaEntry
    {
        internal const string SourceArea = "b10e86516b6437d45a629931c30936fe";
        internal const string DestinationArea = "ead426a6c23d39548a670ee515d77df4";
        internal const string Location = "758559f44d15fc844bf30a10a83154d5";
        internal const string Entry = "fd225a93828d26449803a4847947f8c1";

        internal static BlueprintAreaEnterPoint Resolve(Game game, RuntimeRequest request, out JObject observation)
        {
            observation = null;
            var target = request?.PersistenceAreaTarget;
            Require(request?.OwnedPairs?.IsOwnerSeedCheckpointSave == true && request.PersistenceCase == "multi-area-autosave" &&
                request.OwnedPairs.SourceArea == SourceArea && request.PersistenceLoad?.Area == SourceArea && target != null &&
                target.Area == DestinationArea && target.EnterPoint == Entry && target.AutoSaveMode == "AfterEntry" &&
                game?.Player != null && game.Player.GameId == request.PersistenceLoad.GameId && !game.Player.IsInCombat &&
                game.CurrentMode == GameModeType.Default &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == SourceArea,
                "Owner-seed controlled area entry lacks its exact source and destination declaration.");
            var map = game.Player.GlobalMap;
            var location = ResourcesLibrary.TryGetBlueprint<BlueprintLocation>(Location);
            LocationData data;
            Require(map?.Locations != null && location != null && location.AssetGuidThreadSafe == Location &&
                map.Locations.TryGetValue(location, out data) && data != null && ReferenceEquals(data.Blueprint, location) &&
                data.IsSeen && data.IsRevealed && data.IsExplored && !data.IsClosed,
                "The selected stock destination is not an already-present open explored campaign location.");
            // Do not use GetLocationData: it can add a missing map record. The
            // existing LocationData getter applies the campaign's actual entrance.
            data = map.Locations[location];
            var count = map.Locations.Count;
            var resolved = data.AreaEntrance;
            var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(Entry);
            Require(entry != null && ReferenceEquals(resolved, entry) && entry.AssetGuidThreadSafe == Entry &&
                entry.Area != null && entry.Area.AssetGuidThreadSafe == DestinationArea &&
                !ReferenceEquals(entry.Area, game.CurrentlyLoadedArea) && map.Locations.Count == count,
                "The actual visited-location entrance differs from the pinned stock cross-entry destination.");
            observation = new JObject {
                ["contract"] = "owner-seed-visited-area-entry-v1", ["sourceArea"] = SourceArea,
                ["destinationArea"] = DestinationArea, ["locationGuid"] = location.AssetGuidThreadSafe,
                ["locationName"] = location.name, ["seen"] = data.IsSeen, ["revealed"] = data.IsRevealed,
                ["explored"] = data.IsExplored, ["closed"] = data.IsClosed,
                ["entryGuid"] = resolved.AssetGuidThreadSafe, ["entryAreaGuid"] = resolved.Area.AssetGuidThreadSafe,
                ["entryName"] = resolved.name, ["locationCountBefore"] = count, ["locationCountAfter"] = map.Locations.Count,
                ["destinationIsCapital"] = entry.Area.IsCapital,
                ["existingLocationOnly"] = true, ["entryEqualsRegisteredBlueprint"] = true,
                ["navigationAssisted"] = true, ["ordinaryTravelProof"] = false, ["authoredExitProof"] = false,
                ["nativeMode"] = "AfterEntry", ["transferRequests"] = 0, ["saveRequests"] = 0 };
            return entry;
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
    }
}
