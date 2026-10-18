using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Area;
using Kingmaker.View.MapObjects;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Read-only route discovery in the admitted owner-seed world. An authored
    // destination is evidence for selecting a later guarded native transfer;
    // this observation itself performs no interaction, travel or save.
    internal static class Chunk7StockAreaObservation
    {
        private const string SeedArea = "b10e86516b6437d45a629931c30936fe";
        private const int MaximumTransitions = 64;
        private static readonly string[] CandidateEntries =
        {
            "80c140c44a9dfd54ebecd0829d2dd1e8",
            "0fbacd33cce6908409a8d38f92dec7c9",
            "00becd5302e6d1d4a8ad93b6c612b927"
        };

        internal static JObject Capture(Game game)
        {
            if (game?.CurrentlyLoadedArea?.AssetGuidThreadSafe != SeedArea)
                throw new InvalidOperationException("Stock route observation requires the exact admitted owner-seed area.");
            var transitions = UnityEngine.Object.FindObjectsOfType<AreaTransition>() ?? new AreaTransition[0];
            if (transitions.Length > MaximumTransitions)
                throw new InvalidOperationException("Stock route observation exceeded its declared transition bound.");
            var candidates = new JArray();
            foreach (var guid in CandidateEntries)
            {
                var entry = ResourcesLibrary.TryGetBlueprint<BlueprintAreaEnterPoint>(guid);
                candidates.Add(new JObject { ["requestedGuid"] = guid, ["resolved"] = entry != null,
                    ["entryGuid"] = entry?.AssetGuidThreadSafe, ["entryName"] = entry?.name,
                    ["areaGuid"] = entry?.Area?.AssetGuidThreadSafe, ["areaName"] = entry?.Area?.name });
            }
            return new JObject
            {
                ["contract"] = "owner-seed-stock-area-route-observation-v1",
                ["loadedAreaGuid"] = game.CurrentlyLoadedArea.AssetGuidThreadSafe,
                ["loadedAreaName"] = game.CurrentlyLoadedArea.name,
                ["enumeration"] = "loaded active game objects; no inactive or unloaded scene claim",
                ["maximumTransitions"] = MaximumTransitions, ["observedTransitions"] = transitions.Length,
                ["complete"] = true, ["interactionRequests"] = 0, ["travelRequests"] = 0, ["saveRequests"] = 0,
                ["candidates"] = candidates,
                ["transitions"] = new JArray(transitions.OrderBy(t => t.GetInstanceID()).Select(t => new JObject
                {
                    ["instanceId"] = t.GetInstanceID(), ["name"] = t.name,
                    ["enabled"] = t.enabled, ["activeAndEnabled"] = t.isActiveAndEnabled,
                    ["activeInHierarchy"] = t.gameObject.activeInHierarchy,
                    ["position"] = new JObject { ["x"] = t.transform.position.x, ["y"] = t.transform.position.y, ["z"] = t.transform.position.z },
                    ["blueprintGuid"] = t.Blueprint?.AssetGuidThreadSafe,
                    ["entryGuid"] = t.AreaEnterPoint?.AssetGuidThreadSafe,
                    ["destinationAreaGuid"] = t.AreaEnterPoint?.Area?.AssetGuidThreadSafe,
                    ["destinationAreaName"] = t.AreaEnterPoint?.Area?.name,
                    ["autoSaveMode"] = t.AutoSaveMode.ToString()
                }))
            };
        }
    }
}
