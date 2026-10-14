using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallCalibrationScenario
    {
        private NativeArchiveRestorationProbe manualRestoration;
        private Game manualGame;
        private SaveManager manualManager;
        private bool manualObservationBegun;
        private JObject manualSource, manualArrangement;
        private UnitEntityData[] manualActors, manualCharacters;
        private object manualFeature, manualRank, manualFeatureSource;
        private UnitEntityData manualPriorOwner, manualPriorPet;
        private object manualPriorFeature, manualPriorRank, manualPriorSource, manualPriorOwnerDescriptor, manualPriorPetDescriptor, manualPriorOwnerView, manualPriorPetView;
        private JObject manualPriorPairBefore;
        private string SourceArtifactHash => calibration.UsesManualField ? calibration.ManualSource.SourceArtifactSha256 : selection.SourceArtifactSha256;
        private string SourceArchiveHash => calibration.UsesManualField ? calibration.ManualSource.SourceArchiveSha256 : selection.SourceArchiveSha256;

        internal void BeginBeforeLoad()
        {
            Require(calibration.UsesManualField && !manualObservationBegun, "Only the closed Small Manual case observes one cold load.");
            manualGame = Game.Instance; manualManager = manualGame?.SaveManager;
            Require(manualGame != null && manualManager != null && manualGame.CurrentlyLoadedArea == null && !Busy() &&
                authority.MatchesReadOnlySmallManualPlan(request.PersistenceLoad) && authorization.IsActive &&
                authorization.IsExactPersistenceScope(authority) && authorization.AuthorityGeneration == generation &&
                NativePersistenceIsolation.IsExactAuthority(authority) && manualManager.SavePath == root &&
                authority.AdmittedNativeLoadRequests == 0 && authority.AdmittedNativeWriteRequests == 0 &&
                authorization.AuthorizedLoadCount == 0 && authorization.AuthorizedWriteCount == 0,
                "Small Manual observer requires the exact unspent isolated menu authority.");
            manualObservationBegun = true;
            ReadManualSource();
            // Retain ownership before the first fallible native observer install.
            manualRestoration = new NativeArchiveRestorationProbe(persistence);
            manualRestoration.Install();
            evidence["beforeLoad"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["installedBeforeWorldLoad"] = true,
                ["nativeLoads"] = 0, ["nativeWrites"] = 0, ["sourceReadOnly"] = true };
        }

        private bool ReleaseManualLoadObservation()
        {
            if (manualRestoration == null) return true;
            Require(ReferenceEquals(Game.Instance, manualGame) && ReferenceEquals(manualGame.SaveManager, manualManager) &&
                authorization.IsActive && authorization.AuthorityGeneration == generation && authorization.IsExactPersistenceScope(authority) &&
                NativePersistenceIsolation.IsExactAuthority(authority) && manualManager.SavePath == root,
                "Cold Small observer lost its exact Game/manager/authority; custody remains retained.");
            if (evidence["coldNoPreparation"] == null) evidence["coldNoPreparation"] = manualRestoration.Capture();
            if (!TryRelease("Small cold observer", () => manualRestoration.Dispose())) return false;
            manualRestoration = null; return true;
        }

        private void ReadManualSource()
        {
            sourcePath = Path.Combine(Path.GetDirectoryName(root), Chunk7SmallManualSourceSelection.SourceLeaf);
            archivePath = Path.Combine(root, request.PersistenceLoad.FileName);
            var source = new FileInfo(sourcePath); var archive = new FileInfo(archivePath);
            Require(source.Exists && source.Length > 0 && source.Length <= 16 * 1024 * 1024 && (source.Attributes & FileAttributes.ReparsePoint) == 0 &&
                archive.Exists && archive.Length == request.PersistenceLoad.Length && (archive.Attributes & FileAttributes.ReparsePoint) == 0 &&
                Hash(sourcePath) == SourceArtifactHash && Hash(archivePath) == SourceArchiveHash,
                "Exact acquired Small Manual artifact/archive differs from the staged immutable input.");
            sourceLength = source.Length; sourceTicks = source.LastWriteTimeUtc.Ticks; archiveLength = archive.Length; archiveTicks = archive.LastWriteTimeUtc.Ticks;
            var artifact = JObject.Parse(File.ReadAllText(sourcePath));
            manualSource = RequireManualArtifact(request, artifact);
            sourceSelection = (JObject)manualSource["sourceAcquisition"]["selection"].DeepClone();
            evidence["source"] = new JObject { ["runId"] = calibration.ManualSource.SourceRunId, ["artifactSha256"] = SourceArtifactHash,
                ["archiveSha256"] = SourceArchiveHash, ["archiveLeaf"] = request.PersistenceLoad.FileName,
                ["archiveName"] = request.PersistenceLoad.InternalName, ["archiveLength"] = archiveLength,
                ["bodySelection"] = sourceSelection.DeepClone(), ["sameCandidate"] = true, ["unchangedArchive"] = true,
                ["kind"] = calibration.UsesAssistedManualField ? "qualified-assisted-two-owner-acquired-manual" : "qualified-first-owner-acquired-manual", ["acquiredManual"] = manualSource.DeepClone() };
        }

        // Tests execute this exact source admission; a decoded object still does
        // not authorize load. The runner independently qualifies the full lineage.
        internal static JObject RequireManualArtifact(RuntimeRequest request, JObject artifact)
        {
            var plan = request?.SmallCalibration?.ManualSource;
            var o = artifact?["observations"] as JObject;
            Require(request?.SmallCalibration?.UsesManualField == true && plan != null && plan.Validate(request.RunId).Count == 0 && o != null &&
                (string)artifact["runId"] == plan.SourceRunId && (string)artifact["scenario"] == Chunk7AcquiredManualSelection.Scenario &&
                (string)artifact["evidenceKind"] == Chunk7AcquiredManualScenario.EvidenceKind && (string)artifact["status"] == "PASS" &&
                artifact["errors"] is JArray errors && errors.Count == 0 && (string)artifact["commit"] == request.Commit &&
                (string)artifact["dllSha256"] == request.DllSha256 && (string)artifact["dllMvid"] == request.DllMvid,
                "Small source is not the exact successful same-candidate acquired Manual artifact.");
            if (request.SmallCalibration.UsesAssistedManualField) RequireAssistedSmallManual(o, plan);
            else Require((string)o["selection"]?["case"] == Chunk7AcquiredManualSelection.CaseName &&
                (string)o["selection"]?["actorId"] == plan.ActorId && (string)o["selection"]?["petId"] == plan.PetId &&
                o["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)o["selection"]["baseGameOnly"] &&
                o["twoOwners"] == null && (string)o["sourceAcquisition"]?["selection"]?["case"] == Chunk7ManualDerivedSelection.PaidSmallCase &&
                o["sourceAcquisition"]?["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)o["sourceAcquisition"]["selection"]["baseGameOnly"] &&
                (string)o["sourceAcquisition"]?["field"]?["actorId"] == plan.ActorId && (string)o["sourceAcquisition"]?["field"]?["petId"] == plan.PetId &&
                o["sourceAcquisition"]?["nativeWrite"]?["saveBeforeFieldSelection"]?.Type == JTokenType.Boolean && (bool)o["sourceAcquisition"]["nativeWrite"]["saveBeforeFieldSelection"] &&
                o["bodyObservation"] is JObject && o["savedWorld"] is JObject && o["savedOwner"] is JObject &&
                o["afterSaveArrangement"] is JObject && JToken.DeepEquals(o["beforeSaveArrangement"], o["afterSaveArrangement"]),
                "Small Manual source is not the one-owner field derivative of the exact paid ManualDerived acquisition.");
            var w = o["nativeWrite"];
            Require((string)w?["sha256"] == plan.SourceArchiveSha256 && (string)w?["fileName"] == request.PersistenceLoad?.FileName &&
                (string)w?["internalName"] == Chunk7AcquiredManualSelection.OutputName && (string)w?["type"] == "Manual" &&
                (string)w?["gameId"] == request.PersistenceLoad.GameId && (string)w?["gameName"] == request.PersistenceLoad.GameName &&
                (string)w?["area"] == request.PersistenceLoad.Area && w?["autoLevelup"]?.Type == JTokenType.Boolean && !(bool)w["autoLevelup"] &&
                o["cleanup"]?["custodySettled"]?.Type == JTokenType.Boolean && (bool)o["cleanup"]["custodySettled"] &&
                o["cleanup"]?["sourceUnchanged"]?.Type == JTokenType.Boolean && (bool)o["cleanup"]["sourceUnchanged"],
                "Small Manual source lacks its exact native output or settled immutable custody.");
            return o;
        }

        private void StartManualField()
        {
            Require(manualObservationBegun && manualRestoration != null && manualRestoration.Complete,
                "Small Manual load was not observed before native world creation.");
            AssertAuthority(); AssertFieldInput(); RequireNoOwners();
            Require(Game.Instance.CurrentMode == GameModeType.Default && horses.State == HorseCompanionBlueprintState.Registered &&
                horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null &&
                Game.Instance.UI.CharacterBuildController.LevelUpController == null && Game.Instance.UI.CharacterBuildController.Unit == null,
                "The exact loaded Small Manual needs an idle native field and closed builder.");
            var data = persistence.LoadedArchive; var saved = request.PersistenceLoad;
            Require(data != null && data.SchemaVersion == 3 && data.CampaignId == saved.GameId && data.AreaId == saved.Area &&
                data.Pairs.Length == 0 && data.Participations.Length == 0 && data.Slots.Length == 0 && data.Combat == null &&
                persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0 && persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0 &&
                controls.NativeCastRequestCount == 0 && controls.CapturePersistentSlots().Length == 0 &&
                manualRestoration.Capture()["events"] is JArray events && events.Count == 0,
                "Small Manual native load contains mounted custody, writing, preparation or restoration.");
            var current = CaptureManualFieldRoster();
            if (current.Any(value => !value.IsInState || !value.IsInGame || value.View?.AgentASP == null)) return;
            Require(current.All(value => value.IsDirectlyControllable && value.Commands.Empty && !value.IsInCombat),
                "Small Manual full field roster is not idle and directly controllable.");
            var body = NativeSmallRiderBody.Capture(actor, pet); if (body?.NativeRigAvailable != true) return;
            RequireBody(body); Require(JToken.DeepEquals(JObject.FromObject(body), manualSource["bodyObservation"]),
                "Cold Small body, rig or equipment differs from its actual saved field observation.");
            manualActors = current; manualCharacters = Game.Instance.Player.Party.ToArray(); originalActors = current.Where(value => value != actor && value != pet)
                .OrderBy(value => ReferenceEquals(value, Game.Instance.Player.MainCharacter.Value) ? 0 : 1).ThenBy(value => value.UniqueId, StringComparer.Ordinal).ToArray();
            originalParty = Game.Instance.Player.PartyCharacters.Select(value => value.Value.UniqueId).ToArray();
            var savedOwner = manualSource["savedOwner"];
            Require(originalParty.Length > 1 && originalParty.Length <= 6 &&
                originalParty.OrderBy(value => value, StringComparer.Ordinal).SequenceEqual(((JArray)savedOwner["partyCharacters"]).Select(value => (string)value).OrderBy(value => value, StringComparer.Ordinal)) &&
                !Game.Instance.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, actor)), "Small Manual field character membership differs.");
            manualFeature = actor.Descriptor.GetFact(horses.HorseFeature); manualRank = actor.Descriptor.GetFact(horses.LevelRank);
            manualFeatureSource = (manualFeature as Feature)?.Source;
            manualArrangement = CaptureManualArrangement();
            Require(JToken.DeepEquals(manualArrangement, manualSource["afterSaveArrangement"]) &&
                JToken.DeepEquals(CaptureManualResources(), manualSource["resourcesAfterSave"]),
                "Native cold Small load changed saved XP, money, classes or actor resources.");
            if (calibration.UsesAssistedManualField) BindAssistedManualPriorPair();
            scope = new Chunk7NativeWorldScope(request, current);
            RequireManualFieldIdentity();
            evidence["coldNoPreparation"] = manualRestoration.Capture();
            Require(ReleaseManualLoadObservation(), "Small cold observer retains custody before native action input.");
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            Require(originalSelection.All(value => current.Contains(value)) && originalSelection.Distinct().Count() == originalSelection.Length,
                "Small Manual original selection contains foreign actors.");
            pause = Game.Instance.IsPaused; before = Snapshot(); snapshotsBefore = persistence.SnapshotCount; savesBefore = persistence.CompletedSaveCount; castsBefore = controls.NativeCastRequestCount;
            captured = true;
            MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            evidence["processId"] = Process.GetCurrentProcess().Id; evidence["selection"] = Chunk7EvidenceDto.Request(calibration);
            evidence["declaration"] = new JObject { ["profileId"] = declaration.Body.ProfileId, ["nativeInventorySha256"] = declaration.Body.NativeInventorySha256,
                ["nativeCalibrationSha256"] = declaration.NativeCalibrationSha256, ["qualificationStatus"] = "IN PROGRESS", ["pose"] = JObject.FromObject(declaration.Pose),
                ["mountRootOffset"] = JObject.FromObject(declaration.MountRootOffset), ["animatedSeatCorrection"] = JObject.FromObject(declaration.AnimatedSeatCorrection), ["riderEulerOffset"] = JObject.FromObject(declaration.RiderEulerOffset) };
            evidence["before"] = before; evidence["field"] = before.DeepClone(); evidence["authority"] = AuthoritySnapshot();
            evidence["selector"] = new JObject { ["required"] = false, ["openCount"] = 0, ["manualPartyMutation"] = false, ["fieldRestoredFromManual"] = true };
            evidence["sourceWasRemoteAutosave"] = false; evidence["sourceArchiveStillRemote"] = false;
            evidence["smallPoseQualified"] = false; evidence["mountedCombatProven"] = false; evidence["freshMainCampaignProven"] = false; evidence["manualPartyMutation"] = false;
            evidence["manualNativeRoster"] = new JObject { ["mainActorId"] = originalActors[0].UniqueId, ["arrangement"] = manualArrangement.DeepClone(),
                ["resources"] = CaptureManualResources(), ["actors"] = new JArray(current.Select(value => new JObject { ["id"] = value.UniqueId, ["blueprint"] = value.Blueprint.AssetGuid,
                    ["object"] = Id(value), ["descriptor"] = Id(value.Descriptor), ["view"] = Id(value.View) })) };
            evidence["bodyObservation"] = JObject.FromObject(body);
            evidence["profileInventory"] = Chunk7NativeProfileInventory.Capture(new[] { actor, pet });
            Require((string)evidence["profileInventory"]["captureStatus"] == "PASS", "Loaded native Small rig inventory is unavailable.");
            Pass("C7-small-readonly-source"); Pass("C7-small-native-party-placement");
            Game.Instance.IsPaused = false; BeginCalibration();
        }

        private UnitEntityData[] CaptureManualFieldRoster()
        {
            var sourceWorld = (JObject)manualSource["savedWorld"]; var rows = sourceWorld["party"] as JArray;
            var current = Chunk7NativeFieldRoster.Capture(Game.Instance.Player).OrderBy(value => value.UniqueId, StringComparer.Ordinal).ToArray();
            Require(rows != null && rows.Count >= 3 && rows.Count <= 24 && current.Length == rows.Count &&
                current.Select(value => value.UniqueId).Distinct(StringComparer.Ordinal).Count() == current.Length &&
                current.All(value => rows.Count(row => (string)row["id"] == value.UniqueId && (string)row["blueprint"] == value.Blueprint.AssetGuid) == 1) &&
                Game.Instance.Player.MainCharacter.Value?.UniqueId == (string)sourceWorld["mainActorId"],
                "Small Manual full native field roster or main actor differs from the saved world.");
            actor = Exact(calibration.ManualSource.ActorId); pet = Exact(calibration.ManualSource.PetId);
            Require(current.Contains(actor) && current.Contains(pet) && !ReferenceEquals(actor, Game.Instance.Player.MainCharacter.Value) &&
                actor.Blueprint == BlueprintRoot.Instance.CustomCompanion && actor.Descriptor.OriginalSize == Size.Small && actor.Descriptor.State.Size == Size.Small &&
                ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor) && pet.Blueprint == horses.HorseUnit &&
                current.Count(value => value.Descriptor.Pet != null) == (calibration.UsesAssistedManualField ? 2 : 1) &&
                current.Count(value => value.Descriptor.Master.Value != null) == (calibration.UsesAssistedManualField ? 2 : 1),
                "Small Manual does not restore its sole reciprocal native custom owner and Horse.");
            return current;
        }


        internal static void RequireAssistedSmallManual(JObject o, Chunk7SmallManualSourceSelection plan)
        {
            var source = o?["sourceAcquisition"]; var sourceSelection = source?["selection"];
            var pairs = o?["twoOwners"] as JArray; var settledPairs = o?["settledTwoOwners"] as JArray;
            Require((string)o?["selection"]?["case"] == Chunk7AcquiredManualSelection.TwoOwnerCaseName &&
                o["selection"]?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)o["selection"]["baseGameOnly"] &&
                (string)o["selection"]?["actorId"] == plan.ActorId && (string)o["selection"]?["petId"] == plan.PetId &&
                (string)sourceSelection?["case"] == Chunk7ManualDerivedSelection.PaidSmallCase &&
                (string)sourceSelection?["sourceMode"] == Chunk7CumulativeAcquisitionSelection.AssistedSourceMode &&
                sourceSelection?["baseGameOnly"]?.Type == JTokenType.Boolean && (bool)sourceSelection["baseGameOnly"] &&
                source?["nativeWrite"]?["saveBeforeFieldSelection"]?.Type == JTokenType.Boolean && (bool)source["nativeWrite"]["saveBeforeFieldSelection"] &&
                (string)source?["assistedSource"]?["contract"] == "assisted-producer-cold-cumulative-v1" &&
                source?["assistedSource"]?["setupWasAssisted"]?.Type == JTokenType.Boolean && (bool)source["assistedSource"]["setupWasAssisted"] &&
                source?["assistedSource"]?["setupIsOrdinaryAcquisitionProof"]?.Type == JTokenType.Boolean && !(bool)source["assistedSource"]["setupIsOrdinaryAcquisitionProof"] &&
                (string)source?["field"]?["actorId"] == plan.ActorId && (string)source?["field"]?["petId"] == plan.PetId &&
                pairs != null && pairs.Count == 2 && settledPairs != null && settledPairs.Count == 2 &&
                (string)pairs[0]["actorId"] == (string)sourceSelection["priorOwnerId"] && (string)pairs[0]["petId"] == (string)sourceSelection["priorPetId"] &&
                (string)pairs[1]["actorId"] == plan.ActorId && (string)pairs[1]["petId"] == plan.PetId &&
                new[] { (string)pairs[0]["actorId"], (string)pairs[0]["petId"], plan.ActorId, plan.PetId }.Distinct(StringComparer.Ordinal).Count() == 4 &&
                o["bodyObservation"] is JObject && o["savedWorld"] is JObject && o["savedOwner"] is JObject &&
                o["afterSaveArrangement"] is JObject && JToken.DeepEquals(o["beforeSaveArrangement"], o["afterSaveArrangement"]),
                "Assisted Small Manual requires the exact assisted-cold cumulative paid Small derivative and both retained pairs.");
            foreach (var key in new[] { "actorId", "petId", "actorBlueprint", "petBlueprint", "featureGuid", "sourceGuid", "rank" })
                for (var i = 0; i < 2; i++) Require(JToken.DeepEquals(pairs[i][key], settledPairs[i][key]), "Assisted Small saved pair semantics changed.");
        }
        private void BindAssistedManualPriorPair()
        {
            Require(manualPriorOwner == null && manualPriorPet == null, "Assisted Small prior pair already bound.");
            var prior = manualSource["twoOwners"][0];
            manualPriorOwner = manualActors.SingleOrDefault(value => value.UniqueId == (string)prior["actorId"]);
            manualPriorPet = manualActors.SingleOrDefault(value => value.UniqueId == (string)prior["petId"]);
            Require(manualPriorOwner != null && manualPriorPet != null && manualPriorOwner != actor && manualPriorOwner != pet &&
                manualPriorPet != actor && manualPriorPet != pet && manualPriorOwner != manualPriorPet,
                "Assisted Small prior pair is absent or aliases the calibrated pair.");
            manualPriorOwnerDescriptor = manualPriorOwner.Descriptor; manualPriorPetDescriptor = manualPriorPet.Descriptor;
            manualPriorOwnerView = manualPriorOwner.View; manualPriorPetView = manualPriorPet.View;
            manualPriorFeature = manualPriorOwner.Descriptor.GetFact(horses.HorseFeature);
            manualPriorRank = manualPriorOwner.Descriptor.GetFact(horses.LevelRank);
            manualPriorSource = (manualPriorFeature as Feature)?.Source;
            manualPriorPairBefore = CaptureAssistedManualPriorPair();
            RequireAssistedManualPriorPair();
            evidence["inheritedAssistedPairBefore"] = manualPriorPairBefore.DeepClone();
        }
        private JObject CaptureAssistedManualPriorPair() => new JObject {
            ["actorId"] = manualPriorOwner.UniqueId, ["petId"] = manualPriorPet.UniqueId,
            ["actorObject"] = Id(manualPriorOwner), ["petObject"] = Id(manualPriorPet),
            ["actorDescriptor"] = Id(manualPriorOwner.Descriptor), ["petDescriptor"] = Id(manualPriorPet.Descriptor),
            ["actorView"] = Id(manualPriorOwner.View), ["petView"] = Id(manualPriorPet.View),
            ["featureObject"] = Id(manualPriorFeature), ["sourceObject"] = Id(manualPriorSource), ["rankObject"] = Id(manualPriorRank),
            ["actorBlueprint"] = manualPriorOwner.Blueprint.AssetGuid, ["petBlueprint"] = manualPriorPet.Blueprint.AssetGuid,
            ["featureGuid"] = horses.HorseFeature.AssetGuid, ["sourceGuid"] = (manualPriorFeature as Feature)?.Source?.AssetGuid,
            ["rank"] = manualPriorOwner.Descriptor.GetFact(horses.LevelRank)?.GetRank(),
            ["reciprocal"] = ReferenceEquals(manualPriorOwner.Descriptor.Pet, manualPriorPet) && ReferenceEquals(manualPriorPet.Descriptor.Master.Value, manualPriorOwner),
            ["resources"] = new JArray(((JArray)CaptureManualResources()["actors"]).Where(value =>
                (string)value["actorId"] == manualPriorOwner.UniqueId || (string)value["actorId"] == manualPriorPet.UniqueId).Select(value => value.DeepClone())) };
        private void RequireAssistedManualPriorPair()
        {
            var saved = manualSource["twoOwners"][0];
            Require(manualPriorOwner != null && manualPriorPet != null &&
                ReferenceEquals(manualPriorOwner.Descriptor, manualPriorOwnerDescriptor) && ReferenceEquals(manualPriorPet.Descriptor, manualPriorPetDescriptor) &&
                ReferenceEquals(manualPriorOwner.View, manualPriorOwnerView) && ReferenceEquals(manualPriorPet.View, manualPriorPetView) &&
                ReferenceEquals(manualPriorOwner.Descriptor.GetFact(horses.HorseFeature), manualPriorFeature) &&
                ReferenceEquals(manualPriorOwner.Descriptor.GetFact(horses.LevelRank), manualPriorRank) &&
                manualPriorFeature is Feature feature && ReferenceEquals(feature.Source, manualPriorSource) && manualPriorSource != null &&
                ReferenceEquals(manualPriorOwner.Descriptor.Pet, manualPriorPet) && ReferenceEquals(manualPriorPet.Descriptor.Master.Value, manualPriorOwner) &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, manualPriorOwner)) == 1 &&
                !Game.Instance.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, manualPriorOwner)),
                "Assisted Small inherited native pair/fact/rank/view or character custody changed.");
            var now = CaptureAssistedManualPriorPair();
            foreach (var key in new[] { "actorId", "petId", "actorBlueprint", "petBlueprint", "featureGuid", "sourceGuid", "rank" })
                Require(JToken.DeepEquals(now[key], saved[key]), "Assisted Small inherited pair differs from its saved source.");
            Require(JToken.DeepEquals(now, manualPriorPairBefore), "Assisted Small inherited pair identity or native resources changed.");
            evidence["inheritedAssistedPairAfter"] = now;
        }

        private JObject CaptureManualArrangement() => new JObject { ["money"] = Game.Instance.Player.Money, ["experienceRatePercent"] = Game.Instance.Player.ExperienceRatePercent,
            ["actors"] = new JArray(manualActors.OrderBy(value => value.UniqueId, StringComparer.Ordinal).Select(value => new JObject {
                ["actorId"] = value.UniqueId, ["blueprint"] = value.Blueprint.AssetGuid, ["experience"] = value.Descriptor.Progression.Experience,
                ["characterLevel"] = value.Descriptor.Progression.CharacterLevel,
                ["classes"] = new JArray(value.Descriptor.Progression.Classes.OrderBy(row => row.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(row => new JObject { ["classGuid"] = row.CharacterClass.AssetGuid, ["level"] = row.Level })) })) };
        private JObject CaptureManualResources() => new JObject { ["actors"] = new JArray(manualActors.OrderBy(value => value.UniqueId, StringComparer.Ordinal).Select(value => new JObject {
            ["actorId"] = value.UniqueId, ["move"] = value.CombatState.Cooldown.MoveAction, ["standard"] = value.CombatState.Cooldown.StandardAction,
            ["swift"] = value.CombatState.Cooldown.SwiftAction, ["prepared"] = value.CombatState.Prepared,
            ["reactions"] = value.CombatState.AttackOfOpportunityCount, ["reactionAllowance"] = value.CombatState.AttackOfOpportunityPerRound,
            ["initiative"] = value.CombatState.Initiative })) };
        private void RequireManualFieldIdentity()
        {
            if (calibration.UsesAssistedManualField || manualPriorOwner != null || manualPriorPet != null)
            {
                Require(calibration.UsesAssistedManualField, "Bound assisted Small source case changed; inherited custody remains retained.");
                RequireAssistedManualPriorPair();
            }
            var prior = manualSource["savedOwner"]; var fact = actor.Descriptor.GetFact(horses.HorseFeature) as Feature;
            var current = Chunk7NativeFieldRoster.Capture(Game.Instance.Player);
            Require(manualActors != null && manualActors.Length >= 3 && current.Length == manualActors.Length &&
                current.All(value => manualActors.Count(item => ReferenceEquals(item, value)) == 1) &&
                manualCharacters != null && Game.Instance.Player.Party.Count() == manualCharacters.Length &&
                Game.Instance.Player.Party.All(value => manualCharacters.Count(item => ReferenceEquals(item, value)) == 1) &&
                ReferenceEquals(Game.Instance.Player.MainCharacter.Value, originalActors[0]) &&
                actor.UniqueId == calibration.ManualSource.ActorId && pet.UniqueId == calibration.ManualSource.PetId &&
                ReferenceEquals(actor.Descriptor.Pet, pet) && ReferenceEquals(pet.Descriptor.Master.Value, actor) &&
                fact != null && ReferenceEquals(fact, manualFeature) && fact.Source != null && ReferenceEquals(fact.Source, manualFeatureSource) &&
                fact.Source.AssetGuid == (string)prior["sourceGuid"] && horses.HorseFeature.AssetGuid == (string)prior["featureGuid"] &&
                ReferenceEquals(actor.Descriptor.GetFact(horses.LevelRank), manualRank) && actor.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)prior["rank"] &&
                JToken.DeepEquals(manualArrangement, CaptureManualArrangement()),
                "Small Manual exact owner/fact/rank, full native roster or progression changed; custody remains retained.");
        }
    }
}
