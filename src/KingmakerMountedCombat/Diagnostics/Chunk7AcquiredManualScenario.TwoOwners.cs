using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AcquiredManualScenario
    {
        private bool TwoOwners => selection.Case == Chunk7AcquiredManualSelection.TwoOwnerCaseName;
        private bool PostFieldManual => selection.UsesPostFieldManual;
        private UnitEntityData priorOwner, priorPet;
        private object priorDescriptor, priorPetDescriptor, priorView, priorPetView, priorFeature, priorFeatureSource, priorRank;
        private bool priorBound;
        private JObject priorSource;

        private void RequireTwoOwnerSource()
        {
            if (!TwoOwners) return;
            var selected = sourceObservations["selection"]; var retained = sourceObservations["cumulative"];
            priorSource = retained?["settledPriorOwner"] as JObject;
            Require((string)selected?["priorOwnerId"] == selection.PriorOwnerId && (string)selected?["priorPetId"] == selection.PriorPetId &&
                priorSource != null && (string)priorSource["actorId"] == selection.PriorOwnerId && (string)priorSource["petId"] == selection.PriorPetId &&
                (string)retained["contract"] == "native-one-owner-to-two-v1" && (int?)retained["priorOwnerCount"] == 1 &&
                (int?)retained["maximumOwnerCount"] == 2 && (int?)retained["finalOwnerCount"] == 2 &&
                retained["capturedBeforeObserverRelease"]?.Type == JTokenType.Boolean && (bool)retained["capturedBeforeObserverRelease"] &&
                retained["unrelatedResourcesAfter"]?["actors"] is JArray && retained["relationshipsAfter"]?["actors"] is JArray &&
                retained["progressionAfter"]?["actors"] is JArray && sourceWorld["party"] is JArray roster &&
                roster.Count(row => (string)row["id"] == selection.PriorOwnerId) == 1 && roster.Count(row => (string)row["id"] == selection.PriorPetId) == 1,
                "Two-owner Manual requires its exact completed cumulative source and retained prior pair.");
        }
        private void BindPriorOwner()
        {
            if (!TwoOwners) return;
            Require(!priorBound, "Prior acquired owner may bind only once.");
            priorOwner = actors.SingleOrDefault(value => value.UniqueId == selection.PriorOwnerId);
            priorPet = actors.SingleOrDefault(value => value.UniqueId == selection.PriorPetId);
            Require(priorOwner != null && priorPet != null && priorOwner != acquiredActor && priorOwner != acquiredPet &&
                priorPet != acquiredActor && priorPet != acquiredPet && priorOwner.Blueprint?.AssetGuid == (string)priorSource["actorBlueprint"] &&
                priorPet.Blueprint == horses.HorseUnit && priorPet.Blueprint.AssetGuid == (string)priorSource["petBlueprint"],
                "The exact prior pair did not survive the cumulative native cold load.");
            priorDescriptor = priorOwner.Descriptor; priorPetDescriptor = priorPet.Descriptor;
            priorView = priorOwner.View; priorPetView = priorPet.View;
            priorFeature = priorOwner.Descriptor.GetFact(horses.HorseFeature); priorRank = priorOwner.Descriptor.GetFact(horses.LevelRank);
            priorFeatureSource = (priorFeature as Feature)?.Source; priorBound = true;
            RequireTwoOwnerReferences(); RequireTwoOwnerValues();
            evidence["loadedPriorOwner"] = CaptureOwner(priorOwner, priorPet, (Feature)priorFeature, priorRank);
        }
        private void RequireTwoOwnerReferences()
        {
            if (!TwoOwners || !priorBound) return;
            Require(ReferenceEquals(priorOwner.Descriptor, priorDescriptor) && ReferenceEquals(priorPet.Descriptor, priorPetDescriptor) &&
                ReferenceEquals(priorOwner.View, priorView) && ReferenceEquals(priorPet.View, priorPetView) &&
                ReferenceEquals(priorOwner.Descriptor.Pet, priorPet) && ReferenceEquals(priorPet.Descriptor.Master.Value, priorOwner) &&
                ReferenceEquals(priorOwner.Descriptor.GetFact(horses.HorseFeature), priorFeature) && priorFeature is Feature feature &&
                ReferenceEquals(feature.Source, priorFeatureSource) && feature.Source != null && feature.Source.AssetGuid == (string)priorSource["sourceGuid"] &&
                horses.HorseFeature.AssetGuid == (string)priorSource["featureGuid"] &&
                ReferenceEquals(priorOwner.Descriptor.GetFact(horses.LevelRank), priorRank) &&
                priorOwner.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)priorSource["rank"] &&
                priorOwner.GetActivePolymorph() == null && priorPet.GetActivePolymorph() == null,
                "Prior native pair/fact/source/rank/descriptor/view custody changed.");
        }
        private void RequireTwoOwnerValues()
        {
            if (!TwoOwners || !priorBound) return;
            RequireTwoOwnerReferences();
            Require(priorOwner.IsInState && priorOwner.IsInGame && priorPet.IsInState && priorPet.IsInGame &&
                priorOwner.Descriptor.State.IsConscious && !priorOwner.Descriptor.State.IsDead && !priorPet.Descriptor.State.IsDead &&
                priorOwner.Commands.Empty && priorPet.Commands.Empty &&
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, priorOwner)) == 1 &&
                !game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, priorOwner)), "Prior pair left its living idle native field membership.");
            var retained = sourceObservations["cumulative"];
            var ids = ((JArray)retained["unrelatedResourcesAfter"]["actors"]).Select(row => (string)row["actorId"]).ToArray();
            Require(ids.Contains(selection.PriorOwnerId) && ids.Contains(selection.PriorPetId), "Cumulative source omitted prior-pair resource custody.");
            var resources = new JObject { ["actors"] = new JArray(((JArray)CaptureResources()["actors"]).Where(row => ids.Contains((string)row["actorId"])).Select(row => row.DeepClone())) };
            var progression = new JObject { ["actors"] = new JArray(((JArray)CaptureNoArrangement()["actors"]).Where(row => ids.Contains((string)row["actorId"])).Select(row => row.DeepClone())) };
            var relationships = new JObject { ["actors"] = new JArray(OwnedActors().Where(value => ids.Contains(value.UniqueId)).Select(value => new JObject {
                ["actorId"] = value.UniqueId, ["petId"] = value.Descriptor.Pet?.UniqueId, ["masterId"] = value.Descriptor.Master.Value?.UniqueId })) };
            Require(JToken.DeepEquals(resources, retained["unrelatedResourcesAfter"]) && JToken.DeepEquals(progression, retained["progressionAfter"]) &&
                JToken.DeepEquals(relationships, retained["relationshipsAfter"]), "Cumulative prior/unrelated resources, progression or relationships changed during native cold load/field/save.");
            var all = OwnedActors();
            Require(all.Count(value => value.Descriptor.Pet != null) == 2 && all.Count(value => value.Descriptor.Master.Value != null) == 2,
                "Two-owner Manual refuses missing, foreign or third native companion relationships.");
        }
        private void CaptureTwoOwners(string key)
        {
            if (!TwoOwners || !priorBound || evidence[key] != null) return;
            RequireTwoOwnerValues(); RequireAcquiredOwner();
            evidence[key] = new JArray(CaptureOwner(priorOwner, priorPet, (Feature)priorFeature, priorRank), CaptureAcquiredOwner());
        }
    }
}
