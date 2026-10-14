using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ManualDerivedScenario
    {
        internal const string CumulativeEvidenceFileName = "chunk7-cumulative-acquisition-evidence.json";
        internal const string CumulativeEvidenceKind = "chunk7-cumulative-acquisition-evidence";
        internal const string CumulativePendingFileName = "chunk7-cumulative-acquisition-cleanup-pending.json";
        internal const string CumulativePendingKind = "chunk7-cumulative-acquisition-cleanup-pending";
        private readonly Chunk7CumulativeAcquisitionSelection cumulative;
        private UnitEntityData priorOwner, priorPet;
        private object priorOwnerDescriptor, priorPetDescriptor, priorOwnerView, priorPetView, priorFeature, priorFeatureSource, priorRank;
        private JObject cumulativeSourceOwner, cumulativeInitialResources, cumulativeInitialRelationships, cumulativeInitialProgression;
        private bool cumulativeBound;
        private string SelectedRow(string row) => cumulative == null ? row : row.Replace("C7-manual-derived-", "C7-cumulative-");

        internal static Chunk7ManualDerivedScenario CreateCumulative(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship, NativeMountedControlService controls,
            MountedPersistenceService persistence, HorseCompanionBlueprintService horses)
            => new Chunk7ManualDerivedScenario(request, authorization, authority, relationship, controls, persistence, horses, request?.CumulativeAcquisition);

        private Chunk7ManualDerivedScenario(RuntimeRequest request, RuntimeSaveAuthorization authorization,
            PersistenceSaveAuthorization authority, GameMountedRelationshipService relationship,
            NativeMountedControlService controls, MountedPersistenceService persistence, HorseCompanionBlueprintService horses,
            Chunk7CumulativeAcquisitionSelection cumulative)
        {
            if (request == null || cumulative == null || !ReferenceEquals(request.CumulativeAcquisition, cumulative) || request.ManualDerived != null ||
                request.Scenario != Chunk7CumulativeAcquisitionSelection.Scenario || request.Validate().Count != 0)
                throw new ArgumentException("An exact separate one-owner-to-two-owner cumulative request is required.");
            this.request = request; this.cumulative = cumulative; selection = cumulative.NativeChoicePins();
            this.authorization = authorization ?? throw new ArgumentNullException(nameof(authorization));
            this.authority = authority ?? throw new ArgumentNullException(nameof(authority));
            world = relationship?.World ?? throw new ArgumentNullException(nameof(relationship));
            this.controls = controls ?? throw new ArgumentNullException(nameof(controls));
            this.persistence = persistence ?? throw new ArgumentNullException(nameof(persistence));
            this.horses = horses ?? throw new ArgumentNullException(nameof(horses));
            game = Game.Instance ?? throw new InvalidOperationException("Pre-load Game is unavailable.");
            manager = game.SaveManager; menuPlayer = game.Player; generation = authorization.AuthorityGeneration;
            root = Path.Combine(Path.GetDirectoryName(Path.GetDirectoryName(request.EvidenceRoot)), "runtime-staging", "persistence-" + request.RunId, "Saved Games");
        }
        private bool MatchesSelectedPlan(bool unwritten) => cumulative == null
            ? authority.MatchesManualDerivedPlan(request.PersistenceLoad, Chunk7ManualDerivedSelection.OutputLeaf, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), unwritten)
            : cumulative.UsesAssistedCold
                ? authority.MatchesAssistedCumulativePlan(request, Chunk7CumulativeAcquisitionSelection.OutputLeaf, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), unwritten)
                : authority.MatchesCumulativeAcquisitionPlan(request.PersistenceLoad, Chunk7CumulativeAcquisitionSelection.OutputLeaf, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto), unwritten);

        private void ReadCumulativeSource()
        {
            if (cumulative.UsesAssistedCold) { ReadAssistedCumulativeSource(); return; }
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7CumulativeAcquisitionSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == cumulative.SourceArtifactSha256 && Hash(archivePath) == cumulative.SourceArchiveSha256,
                "Exact acquired Manual artifact/archive bytes differ.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks; sourcePinned = true;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath)); sourceObservations = artifact["observations"] as JObject;
            Require((string)artifact["runId"] == cumulative.SourceRunId && (string)artifact["scenario"] == Chunk7AcquiredManualSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7AcquiredManualScenario.EvidenceKind && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray errors && errors.Count == 0 && (string)artifact["commit"] == request.Commit &&
                (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid && sourceObservations != null &&
                sourceObservations["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)sourceObservations["selection"]["baseGameOnly"] &&
                (string)sourceObservations["selection"]?["case"] == Chunk7AcquiredManualSelection.CaseName &&
                (string)sourceObservations["nativeWrite"]?["sha256"] == cumulative.SourceArchiveSha256 &&
                (string)sourceObservations["nativeWrite"]?["gameId"] == request.PersistenceLoad.GameId &&
                (string)sourceObservations["nativeWrite"]?["gameName"] == request.PersistenceLoad.GameName &&
                (string)sourceObservations["nativeWrite"]?["area"] == request.PersistenceLoad.Area &&
                (string)sourceObservations["nativeWrite"]?["fileName"] == request.PersistenceLoad.FileName &&
                (string)sourceObservations["nativeWrite"]?["type"] == "Manual" &&
                sourceObservations["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)sourceObservations["cleanup"]["custodySettled"] &&
                sourceObservations["cleanup"]?["sourceUnchanged"]?.Type == JTokenType.Boolean && (bool)sourceObservations["cleanup"]["sourceUnchanged"],
                "Cumulative input requires the exact successful same-candidate first-owner acquired Manual lineage.");
            sourceWorld = sourceObservations["savedWorld"] as JObject; cumulativeSourceOwner = sourceObservations["savedOwner"] as JObject;
            Require(sourceWorld != null && sourceWorld["party"] is JArray party && party.Count >= 2 && party.Count <= 24 &&
                (string)sourceWorld["gameId"] == request.PersistenceLoad.GameId && (string)sourceWorld["area"] == request.PersistenceLoad.Area &&
                cumulativeSourceOwner != null && (string)cumulativeSourceOwner["actorId"] == cumulative.PriorOwnerId && (string)cumulativeSourceOwner["petId"] == cumulative.PriorPetId &&
                (string)sourceObservations["selection"]?["actorId"] == cumulative.PriorOwnerId && (string)sourceObservations["selection"]?["petId"] == cumulative.PriorPetId,
                "Cumulative source has no exact saved first owner/pet graph.");
            expectedMainId = (string)sourceWorld["mainActorId"];
            Require(Chunk7ManualDerivedSelection.NativeId(expectedMainId) && ((JArray)sourceWorld["party"]).Count(row => (string)row["id"] == expectedMainId) == 1 &&
                (string)sourceObservations["fixtureReadiness"]?["captureStatus"] == "PASS" && sourceObservations["afterSaveArrangement"] is JObject &&
                sourceObservations["nativeProfileInventory"]?["party"] is JArray && sourceObservations["resourcesAfterSave"]?["actors"] is JArray,
                "Cumulative input lacks its actual world, native readiness, progression or action-resource observations.");
            var inventory = (JArray)sourceObservations["nativeProfileInventory"]["party"];
            Require(inventory.Count == ((JArray)sourceWorld["party"]).Count && inventory.Count(row => row["petId"]?.Type == JTokenType.String) == 1 &&
                inventory.Count(row => (string)row["id"] == cumulative.PriorOwnerId && (string)row["petId"] == cumulative.PriorPetId) == 1 &&
                inventory.Count(row => (string)row["id"] == cumulative.PriorPetId && (string)row["masterId"] == cumulative.PriorOwnerId) == 1,
                "Cumulative source must contain exactly one reciprocal native companion relationship.");
        }
        private void BindCumulativeOwner()
        {
            if (cumulative == null) return;
            Require(!cumulativeBound, "Cumulative prior owner can bind only once.");
            priorOwner = actors.SingleOrDefault(value => value.UniqueId == cumulative.PriorOwnerId);
            priorPet = actors.SingleOrDefault(value => value.UniqueId == cumulative.PriorPetId);
            Require(priorOwner != null && priorPet != null && !ReferenceEquals(priorOwner, priorPet) &&
                priorOwner.Blueprint?.AssetGuid == (string)cumulativeSourceOwner["actorBlueprint"] && priorPet.Blueprint == horses.HorseUnit &&
                priorPet.Blueprint?.AssetGuid == (string)cumulativeSourceOwner["petBlueprint"] &&
                ReferenceEquals(priorOwner.Descriptor.Pet, priorPet) && ReferenceEquals(priorPet.Descriptor.Master.Value, priorOwner) &&
                actors.Count(value => value.Descriptor.Pet != null) == 1 && actors.Count(value => value.Descriptor.Master.Value != null) == 1 &&
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, priorOwner)) == 1 &&
                !game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, priorOwner)),
                "Native cold load did not restore precisely one field owner and its reciprocal Horse.");
            priorOwnerDescriptor = priorOwner.Descriptor; priorPetDescriptor = priorPet.Descriptor;
            priorOwnerView = priorOwner.View; priorPetView = priorPet.View;
            priorFeature = priorOwner.Descriptor.GetFact(horses.HorseFeature); priorRank = priorOwner.Descriptor.GetFact(horses.LevelRank);
            priorFeatureSource = (priorFeature as Feature)?.Source;
            cumulativeBound = true; RequireCumulativeReferences();
            var resources = CaptureCumulativeResources(false);
            Require(JToken.DeepEquals(resources, SelectedSourceResources), "Cold cumulative native action/reaction resources differ from the exact saved source.");
            cumulativeInitialResources = CaptureCumulativeResources(true);
            cumulativeInitialRelationships = CaptureCumulativeRelationships();
            cumulativeInitialProgression = CaptureCumulativeProgression();
            evidence["cumulative"] = new JObject { ["contract"] = "native-one-owner-to-two-v1", ["priorOwnerCount"] = 1,
                ["maximumOwnerCount"] = 2, ["sourceOwner"] = cumulativeSourceOwner.DeepClone(), ["loadedPriorOwner"] = CapturePriorOwner(),
                ["resourcesLoaded"] = resources, ["unrelatedResourcesBefore"] = cumulativeInitialResources.DeepClone(),
                ["relationshipsBefore"] = cumulativeInitialRelationships.DeepClone(), ["progressionBefore"] = cumulativeInitialProgression.DeepClone(),
                ["noAcquisitionGrants"] = true, ["generalizedCumulativeAdmission"] = false };
        }
        private UnitEntityData[] CumulativeUnrelatedActors() => actors.Where(value => value.UniqueId != selection.ExistingActorId)
            .OrderBy(value => value.UniqueId, StringComparer.Ordinal).ToArray();
        private JObject CaptureCumulativeResources(bool unrelated) => new JObject { ["actors"] = new JArray((unrelated ? CumulativeUnrelatedActors() : actors.OrderBy(value => value.UniqueId, StringComparer.Ordinal).ToArray()).Select(value => new JObject {
            ["actorId"] = value.UniqueId, ["move"] = value.CombatState.Cooldown.MoveAction, ["standard"] = value.CombatState.Cooldown.StandardAction,
            ["swift"] = value.CombatState.Cooldown.SwiftAction, ["prepared"] = value.CombatState.Prepared,
            ["reactions"] = value.CombatState.AttackOfOpportunityCount, ["reactionAllowance"] = value.CombatState.AttackOfOpportunityPerRound,
            ["initiative"] = value.CombatState.Initiative })) };
        private JObject CaptureCumulativeRelationships() => new JObject { ["actors"] = new JArray(CumulativeUnrelatedActors().Select(value => new JObject {
            ["actorId"] = value.UniqueId, ["petId"] = value.Descriptor.Pet?.UniqueId, ["masterId"] = value.Descriptor.Master.Value?.UniqueId })) };
        private JObject CaptureCumulativeProgression() => new JObject { ["actors"] = new JArray(((JArray)CaptureNoArrangement()["actors"])
            .Where(value => (string)value["actorId"] != selection.ExistingActorId).Select(value => value.DeepClone())) };
        private void RequireCumulativeReferences()
        {
            if (cumulative == null || !cumulativeBound) return;
            Require(ReferenceEquals(priorOwner.Descriptor, priorOwnerDescriptor) && ReferenceEquals(priorPet.Descriptor, priorPetDescriptor) &&
                ReferenceEquals(priorOwner.View, priorOwnerView) && ReferenceEquals(priorPet.View, priorPetView) &&
                ReferenceEquals(priorOwner.Descriptor.Pet, priorPet) && ReferenceEquals(priorPet.Descriptor.Master.Value, priorOwner) &&
                ReferenceEquals(priorOwner.Descriptor.GetFact(horses.HorseFeature), priorFeature) && priorFeature is Feature fact &&
                ReferenceEquals(fact.Source, priorFeatureSource) && fact.Source != null && fact.Source.AssetGuid == (string)cumulativeSourceOwner["sourceGuid"] &&
                horses.HorseFeature.AssetGuid == (string)cumulativeSourceOwner["featureGuid"] &&
                ReferenceEquals(priorOwner.Descriptor.GetFact(horses.LevelRank), priorRank) &&
                priorOwner.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)cumulativeSourceOwner["rank"] &&
                priorOwner.GetActivePolymorph() == null && priorPet.GetActivePolymorph() == null,
                "Captured prior owner/pet, feature source, rank, descriptor or view authority changed.");
        }
        private void RequireCumulativeValues()
        {
            if (cumulative == null || !cumulativeBound) return;
            RequireCumulativeReferences();
            Require(priorOwner.IsInState && priorOwner.IsInGame && priorPet.IsInState && priorPet.IsInGame &&
                priorOwner.Descriptor.State.IsConscious && !priorOwner.Descriptor.State.IsDead && !priorPet.Descriptor.State.IsDead &&
                priorOwner.Commands.Empty && priorPet.Commands.Empty && game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, priorOwner)) == 1,
                "Prior owner/pet is no longer the same living settled field relationship.");
            Require(JToken.DeepEquals(cumulativeInitialResources, CaptureCumulativeResources(true)) &&
                JToken.DeepEquals(cumulativeInitialRelationships, CaptureCumulativeRelationships()) &&
                JToken.DeepEquals(cumulativeInitialProgression, CaptureCumulativeProgression()),
                "Unrelated original actors lost exact action/reaction, relationship or progression state.");
        }
        private JObject CapturePriorOwner() => new JObject { ["actorId"] = priorOwner.UniqueId, ["petId"] = priorPet.UniqueId,
            ["actorObject"] = probe.Identity(priorOwner), ["actorDescriptor"] = probe.Identity(priorOwner.Descriptor), ["actorView"] = probe.Identity(priorOwner.View),
            ["petObject"] = probe.Identity(priorPet), ["petDescriptor"] = probe.Identity(priorPet.Descriptor), ["petView"] = probe.Identity(priorPet.View),
            ["featureObject"] = probe.Identity(priorFeature), ["sourceObject"] = probe.Identity(priorFeatureSource), ["rankObject"] = probe.Identity(priorRank),
            ["actorBlueprint"] = priorOwner.Blueprint.AssetGuid, ["petBlueprint"] = priorPet.Blueprint.AssetGuid,
            ["featureGuid"] = horses.HorseFeature.AssetGuid, ["sourceGuid"] = ((Feature)priorFeature).Source.AssetGuid,
            ["rank"] = priorOwner.Descriptor.GetFact(horses.LevelRank).GetRank(), ["reciprocal"] = true };
        private void CaptureCumulativeFinal()
        {
            if (cumulative == null || !cumulativeBound || evidence["cumulative"]?["settledPriorOwner"] != null) return;
            RequireCumulativeValues();
            var row = (JObject)evidence["cumulative"];
            row["settledPriorOwner"] = CapturePriorOwner(); row["unrelatedResourcesAfter"] = CaptureCumulativeResources(true);
            row["relationshipsAfter"] = CaptureCumulativeRelationships(); row["progressionAfter"] = CaptureCumulativeProgression();
            row["finalOwnerCount"] = actors.Concat(new[] { acquiredActor, acquiredPet }).Where(value => value != null).Distinct().Count(value => value.Descriptor.Pet != null);
            Require(!acquisitionProven || (int)row["finalOwnerCount"] == 2 && acquiredActor != priorOwner && acquiredActor != priorPet && acquiredPet != priorOwner && acquiredPet != priorPet,
                "Completed cumulative acquisition must retain exactly the prior pair plus one new reciprocal pair.");
            row["capturedBeforeObserverRelease"] = true;
        }
    }
}
