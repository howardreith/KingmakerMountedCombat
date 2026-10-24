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
            LocationData data = null;
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

    // The door candidate is a different revealed, unvisited stock location.
    // Keep its exact entrance and provenance separate from Manticore evidence.
    internal static class Chunk7OwnerSeedTombEntry
    {
        internal const string SourceArea = Chunk7OwnerSeedManticoreEntry.SourceArea;
        internal const string Location = "38638e43953432e42a037956f798f15e";
        internal const string Entry = "c23675e173391ad4297dbe3720122bca";

        internal static BlueprintAreaEnterPoint Resolve(Game game, RuntimeRequest request, out JObject observation)
        {
            observation = null;
            Require(request?.OwnedPairs?.IsOwnerSeedCheckpointSave == true &&
                request.PersistenceCase == Chunk7OwnedPairsSelection.TombFixtureCheckpoint &&
                request.PersistenceLoad?.Sha256 == Chunk7OwnedPairsSelection.TombFixtureSourceSha256 &&
                request.PersistenceLoad.Area == SourceArea && request.PersistenceAreaTarget == null &&
                request.PersistenceReplayTargets == null && game?.Player != null &&
                game.Player.GameId == request.PersistenceLoad.GameId && !game.Player.IsInCombat &&
                game.CurrentMode == GameModeType.Default && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == SourceArea,
                "Tomb fixture preparation requires its exact accepted source and closed native route.");
            var map = game.Player.GlobalMap;
            var location = ResourcesLibrary.TryGetBlueprint<BlueprintLocation>(Location);
            LocationData data = null;
            Require(map?.Locations != null && location != null && location.AssetGuidThreadSafe == Location &&
                map.Locations.TryGetValue(location, out data) && data != null && ReferenceEquals(data.Blueprint, location) &&
                data.IsSeen && data.IsRevealed && !data.IsClosed && !data.IsExplored,
                "Tomb fixture preparation requires its existing revealed/open, still unexplored saved location.");
            var count = map.Locations.Count;
            var resolved = data.AreaEntrance;
            var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(Entry);
            Require(entry != null && ReferenceEquals(resolved, entry) && entry.AssetGuidThreadSafe == Entry &&
                entry.Area != null && entry.Area.AssetGuidThreadSafe == Chunk7OwnedPairsSelection.TombFixtureArea &&
                !entry.Area.IsCapital && !ReferenceEquals(entry.Area, game.CurrentlyLoadedArea) && map.Locations.Count == count,
                "Tomb fixture preparation refused a different actual entrance or a capital destination.");
            observation = new JObject {
                ["contract"] = "owner-seed-tomb-entry-v1", ["sourceArea"] = SourceArea,
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

    // The same immutable owner seed exposes one final revealed field entrance.
    // This only reads the saved row and uses its actual native area entry.
    internal static class Chunk7OwnerSeedThornRiverEntry
    {
        internal const string SourceArea = Chunk7OwnerSeedManticoreEntry.SourceArea;
        internal const string Location = "e929000884b47804f9c026c873aaa57b";
        internal const string Entry = "1ab738bc965352a479b68d1bd325c810";

        internal static BlueprintAreaEnterPoint Resolve(Game game, RuntimeRequest request, out JObject observation)
        {
            observation = null;
            Require(request?.OwnedPairs?.IsOwnerSeedCheckpointSave == true &&
                request.PersistenceCase == Chunk7OwnedPairsSelection.ThornRiverFixtureCheckpoint &&
                request.PersistenceLoad?.Sha256 == Chunk7OwnedPairsSelection.ThornRiverFixtureSourceSha256 &&
                request.PersistenceLoad.Area == SourceArea && request.PersistenceAreaTarget == null &&
                request.PersistenceReplayTargets == null && game?.Player != null &&
                game.Player.GameId == request.PersistenceLoad.GameId && !game.Player.IsInCombat &&
                game.CurrentMode == GameModeType.Default && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == SourceArea,
                "Thorn River fixture preparation requires its exact accepted source and closed native route.");
            var map = game.Player.GlobalMap;
            var location = ResourcesLibrary.TryGetBlueprint<BlueprintLocation>(Location);
            LocationData data = null;
            Require(map?.Locations != null && location != null && location.AssetGuidThreadSafe == Location &&
                map.Locations.TryGetValue(location, out data) && data != null && ReferenceEquals(data.Blueprint, location) &&
                data.IsSeen && data.IsRevealed && !data.IsClosed && !data.IsExplored,
                "Thorn River requires its existing revealed/open, still unexplored saved location.");
            var count = map.Locations.Count;
            var resolved = data.AreaEntrance;
            var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(Entry);
            Require(entry != null && ReferenceEquals(resolved, entry) && entry.AssetGuidThreadSafe == Entry &&
                entry.Area != null && entry.Area.AssetGuidThreadSafe == Chunk7OwnedPairsSelection.ThornRiverFixtureArea &&
                !entry.Area.IsCapital && !ReferenceEquals(entry.Area, game.CurrentlyLoadedArea) && map.Locations.Count == count,
                "Thorn River refused a different actual entrance or a capital destination.");
            observation = new JObject {
                ["contract"] = "owner-seed-thorn-river-entry-v1", ["sourceArea"] = SourceArea,
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

    // The Manticore Manual is a distinct mounted source. Its return route may
    // use only the already-explored Spider Cave location and its actual stock
    // entrance; reading Locations never creates or edits a map record.
    internal static class Chunk7ManticoreReturnEntry
    {
        internal const string Location = "1ee73aa52dab0264ca0b671d9a593c02";
        internal const string Entry = Chunk7OwnedPairsSelection.ManticoreReturnEntry;
        internal const string Area = Chunk7OwnedPairsSelection.ManticoreReturnArea;

        internal static BlueprintAreaEnterPoint Resolve(Game game, RuntimeRequest request, out JObject observation)
        {
            observation = null;
            var selection = request?.OwnedPairs;
            var target = request?.PersistenceAreaTarget;
            Require(selection?.IsOwnerSeedCheckpointSave == true && selection.IsManticoreReturnArea &&
                request.PersistenceCase == "multi-area-autosave" &&
                selection.SourceArchiveSha256 == Chunk7OwnedPairsSelection.ManticoreFixtureManualSha256 &&
                selection.SourceArea == Chunk7OwnedPairsSelection.ManticoreFixtureArea &&
                request.PersistenceLoad?.Sha256 == selection.SourceArchiveSha256 &&
                request.PersistenceLoad.Area == selection.SourceArea &&
                target != null && target.EnterPoint == Entry && target.Area == Area &&
                target.AutoSaveMode == "AfterEntry" && game?.Player != null &&
                game.Player.GameId == request.PersistenceLoad.GameId && !game.Player.IsInCombat &&
                game.CurrentMode == GameModeType.Default &&
                game.CurrentlyLoadedArea?.AssetGuidThreadSafe == selection.SourceArea,
                "Manticore return lacks its exact mounted source, Spider destination or settled native mode.");
            var map = game.Player.GlobalMap;
            var location = ResourcesLibrary.TryGetBlueprint<BlueprintLocation>(Location);
            LocationData data = null;
            Require(map?.Locations != null && location != null && location.AssetGuidThreadSafe == Location &&
                map.Locations.TryGetValue(location, out data) && data != null && ReferenceEquals(data.Blueprint, location) &&
                data.IsSeen && data.IsRevealed && data.IsExplored && !data.IsClosed,
                "The Spider Cave return is not an existing open explored campaign location.");
            var count = map.Locations.Count;
            var resolved = data.AreaEntrance;
            var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(Entry);
            Require(entry != null && ReferenceEquals(resolved, entry) && entry.AssetGuidThreadSafe == Entry &&
                entry.Area != null && entry.Area.AssetGuidThreadSafe == Area && !entry.Area.IsCapital &&
                !ReferenceEquals(entry.Area, game.CurrentlyLoadedArea) && map.Locations.Count == count,
                "The visited Spider Cave location resolves to another entrance, area or capital.");
            observation = new JObject {
                ["contract"] = "manticore-visited-spider-return-v1", ["sourceArea"] = selection.SourceArea,
                ["destinationArea"] = Area, ["sourceManualSha256"] = selection.SourceArchiveSha256,
                ["locationGuid"] = location.AssetGuidThreadSafe, ["locationName"] = location.name,
                ["seen"] = data.IsSeen, ["revealed"] = data.IsRevealed, ["explored"] = data.IsExplored,
                ["closed"] = data.IsClosed, ["entryGuid"] = resolved.AssetGuidThreadSafe,
                ["entryAreaGuid"] = resolved.Area.AssetGuidThreadSafe, ["entryName"] = resolved.name,
                ["locationCountBefore"] = count, ["locationCountAfter"] = map.Locations.Count,
                ["destinationIsCapital"] = entry.Area.IsCapital, ["existingLocationOnly"] = true,
                ["entryEqualsRegisteredBlueprint"] = true, ["navigationAssisted"] = true,
                ["ordinaryTravelProof"] = false, ["authoredExitProof"] = false,
                ["nativeMode"] = "AfterEntry", ["transferRequests"] = 0, ["saveRequests"] = 0 };
            return entry;
        }

        private static void Require(bool value, string message)
        { if (!value) throw new InvalidOperationException(message); }
    }
}
