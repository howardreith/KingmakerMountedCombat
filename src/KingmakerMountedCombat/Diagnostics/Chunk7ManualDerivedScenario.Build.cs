using System;
using System.IO;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ManualDerivedScenario
    {
        private const string RangerClassGuid = "cda0615668a6df14eb36ba19ee881af6";
        private JObject sourceObservations;
        private Chunk7OrdinaryBuildDriver driver;
        private NativeOrdinaryAcquisitionProbe ordinaryProbe;
        private Chunk7NativeBuilderCancellation cancellation;
        private Chunk7NativePartyFieldAdmission fieldAdmission;
        private UnitEntityData acquiredActor, acquiredPet;
        private object acquiredDescriptor, acquiredView, acquiredPetDescriptor, acquiredPetView;
        private UnitEntityData[] originalSelection;
        private string[] originalPartyCharacters;
        private bool presentationCaptured, originalPause, slotProjectionOwned, buildStarted, buildInputAttempted, acquisitionProven;
        private MovementCaptureOverlayLease ummLease;
        private UnityModManagerNet.UnityModManager.UI ummInstance;
        private bool originalUmmOpened;
        private int buildStage;
        private double buildStageStart;
        private readonly JArray choices = new JArray();

        private void RequireManualReadiness()
        {
            var actual = evidence["fixtureReadiness"]; var prior = SelectedSourceReadiness;
            foreach (var key in new[] { "mainProgression", "staticPartyFacts", "saveSettings", "experienceRatePercent" })
                Require(JToken.DeepEquals(actual[key], prior[key]), "Native Manual acquisition readiness differs from source: " + key);
            Require(JToken.DeepEquals(evidence["nativeProfileInventory"]["ordinaryAcquisitionPrerequisites"],
                SelectedSourceInventory["ordinaryAcquisitionPrerequisites"]), "Native XP, money or party readiness differs from qualified Manual observation.");
            Require(JToken.DeepEquals(CaptureNoArrangement(), SelectedSourceArrangement),
                "Loaded Manual actor/class/XP/money state differs from its observed native source.");
            Require(actual["saveSettings"]?["onlyOneSave"]?.Type == JTokenType.Boolean && !(bool)actual["saveSettings"]["onlyOneSave"] &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled, "Manual acquisition refuses one-save mode or a pre-existing slot projection.");
            RequireLiveEligibility();
        }
        private void RequireLiveEligibility()
        {
            var player = game.Player; var main = player.MainCharacter.Value;
            var table = BlueprintRoot.Instance.Progression.XPTable;
            Require(table?.Bonuses != null && table.Bonuses.Length >= 21, "Native XP table lacks bounded indices zero through twenty.");
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(RangerClassGuid);
            Require(ranger != null, "Exact native Ranger class is unavailable.");
            var target = selection.Case == Chunk7ManualDerivedSelection.ExistingCase ? actors.SingleOrDefault(actor => actor.UniqueId == selection.ExistingActorId) : null;
            var actual = new Chunk7ManualDerivedNativeReadiness {
                MainActorId = main.UniqueId, MainLevel = main.Descriptor.Progression.CharacterLevel,
                MainExperience = main.Descriptor.Progression.Experience, Money = player.Money,
                ExperienceRatePercent = player.ExperienceRatePercent, NativeHireCost = player.GetCustomCompanionCost(),
                DefaultHireExperience = table.GetBonus(main.Descriptor.Progression.CharacterLevel), XpThresholds = table.Bonuses.Take(21).ToArray(),
                PartyCharacterIds = player.PartyCharacters.Select(value => value.Value.UniqueId).ToArray(),
                ExistingActorId = target?.UniqueId, ExistingLevel = target?.Descriptor.Progression.CharacterLevel ?? 0,
                ExistingExperience = target?.Descriptor.Progression.Experience ?? 0, ExistingRangerLevel = target?.Descriptor.Progression.GetClassLevel(ranger) ?? 0,
                TargetLiving = target != null && target.Descriptor.State.IsConscious && !target.Descriptor.State.IsDead,
                TargetIsPet = target?.Descriptor.Master.Value != null, TargetHasPet = target?.Descriptor.Pet != null,
                TargetHasDualCompanion = target?.Get<UnitPartDualCompanion>() != null,
                TargetInField = target != null && target.IsInState && target.IsInGame && target.IsDirectlyControllable && target.View?.AgentASP != null,
                TargetIdle = target != null && target.Commands.Empty && !target.IsInCombat,
                TargetCanLevelUp = target != null && LevelUpController.CanLevelUp(target.Descriptor),
                ExactSmallCatalogueMember = selection.Case == Chunk7ManualDerivedSelection.PaidSmallCase &&
                    CatalogueContains(SelectedSourceInventory) && CatalogueContains(evidence["nativeProfileInventory"]),
                ExactMediumBodyMember = selection.Case == Chunk7ManualDerivedSelection.PaidMediumCase &&
                    Chunk7MediumHireBodySource.Matches(SelectedSourceInventory, selection.MediumBody) &&
                    Chunk7MediumHireBodySource.Matches(evidence["nativeProfileInventory"], selection.MediumBody) && NativeMediumBodyMatches()
            };
            var failures = Chunk7ManualDerivedAdmission.Validate(selection, request.RunId, actual);
            Require(failures.Count == 0, "Native Manual-derived acquisition is ineligible: " + string.Join(" | ", failures));
            evidence["liveEligibility"] = new JObject { ["nativeXpThresholds"] = new JArray(actual.XpThresholds),
                ["mainActorId"] = actual.MainActorId, ["mainLevel"] = actual.MainLevel, ["mainExperience"] = actual.MainExperience,
                ["money"] = actual.Money, ["experienceRatePercent"] = actual.ExperienceRatePercent,
                ["nativeHireCost"] = actual.NativeHireCost, ["defaultHireExperience"] = actual.DefaultHireExperience,
                ["partyCharacterIds"] = new JArray(actual.PartyCharacterIds), ["existingActorId"] = actual.ExistingActorId,
                ["existingLevel"] = actual.ExistingLevel, ["existingExperience"] = actual.ExistingExperience, ["existingRangerLevel"] = actual.ExistingRangerLevel,
                ["targetLiving"] = actual.TargetLiving, ["targetIsPet"] = actual.TargetIsPet, ["targetHasPet"] = actual.TargetHasPet,
                ["targetHasDualCompanion"] = actual.TargetHasDualCompanion, ["targetInField"] = actual.TargetInField,
                ["targetIdle"] = actual.TargetIdle, ["targetCanLevelUp"] = actual.TargetCanLevelUp,
                ["exactSmallCatalogueMember"] = actual.ExactSmallCatalogueMember, ["arrangementGranted"] = false };
            if (selection.Case == Chunk7ManualDerivedSelection.PaidMediumCase)
                evidence["liveEligibility"]["exactMediumBodyMember"] = actual.ExactMediumBodyMember;
        }
        private bool CatalogueContains(JToken inventory)
        {
            var races = inventory?["smallRaces"] as JArray; if (races == null) return false;
            var race = races.OfType<JObject>().SingleOrDefault(row => (string)row["raceGuid"] == selection.SmallRaceGuid);
            var preset = (race?["presets"] as JArray)?.OfType<JObject>().SingleOrDefault(row => (string)row["presetGuid"] == selection.SmallPresetGuid);
            var skeleton = preset?[selection.Gender == "Male" ? "maleSkeleton" : "femaleSkeleton"];
            return (string)race?["size"] == "Small" && (string)preset?["raceId"] == (string)race?["raceId"] &&
                preset?["present"]?.Type == JTokenType.Boolean && (bool)preset["present"] &&
                skeleton?["present"]?.Type == JTokenType.Boolean && (bool)skeleton["present"] && (string)skeleton["name"] == selection.SkeletonName;
        }
        private JObject CaptureNoArrangement() => new JObject { ["money"] = game.Player.Money, ["experienceRatePercent"] = game.Player.ExperienceRatePercent,
            ["actors"] = new JArray(actors.OrderBy(actor => actor.UniqueId, StringComparer.Ordinal).Select(actor => new JObject {
                ["actorId"] = actor.UniqueId, ["blueprint"] = actor.Blueprint.AssetGuid,
                ["experience"] = actor.Descriptor.Progression.Experience, ["characterLevel"] = actor.Descriptor.Progression.CharacterLevel,
                ["classes"] = new JArray(actor.Descriptor.Progression.Classes.OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(value => new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })) })) };

        private void StartBuild()
        {
            RequireAuthority(false); RequireWorld(); RequireNoOwners(); RequireNoEffects(); VerifySourceBytes();
            Require(!buildStarted && !Busy() && authority.AdmittedNativeWriteRequests == 0 && authorization.AuthorizedWriteCount == 0 &&
                game.CurrentMode == GameModeType.Default && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Manual-derived choices require the exact idle cold-loaded world before any write.");
            Require(game.UI?.CharacterBuildController != null && game.UI.CharacterBuildController.LevelUpController == null &&
                game.UI.CharacterBuildController.Unit == null && (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown),
                "A pre-existing builder or dialog cannot be borrowed.");
            Require(horses.State == HorseCompanionBlueprintState.Registered && horses.HorseFeature != null && horses.HorseUnit != null && horses.LevelRank != null,
                "Exact ordinary Horse registration is not ready.");
            Require(game.SaveManager.IsSaveAllowed() && !game.SaveManager.Any(save => save.Type == SaveInfo.SaveType.Auto) &&
                !NativePersistenceIsolation.NativeSlotRotationEnabled, "Terminal native autosave is unavailable or has a pre-existing slot/projection.");
            RequireLiveEligibility();
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            Require(originalSelection.All(value => actors.Any(actor => ReferenceEquals(actor, value))) && originalSelection.Distinct().Count() == originalSelection.Length,
                "Native pre-choice selection includes an unowned or duplicate actor.");
            originalPause = game.IsPaused;
            originalPartyCharacters = game.Player.PartyCharacters.Select(value => value.Value.UniqueId).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            ummInstance = UnityModManagerNet.UnityModManager.UI.Instance;
            Require(ummInstance != null, "Exact UMM presentation instance is unavailable."); originalUmmOpened = ummInstance.Opened;
            Require(ummLease == null, "An existing presentation lease cannot be overwritten.");
            presentationCaptured = true; MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            var paid = selection.PaidHire;
            var medium = selection.Case == Chunk7ManualDerivedSelection.PaidMediumCase;
            var pins = new Chunk7OrdinaryBuildPins(request.RunId, selection.SourceArtifactSha256,
                medium ? Chunk7OrdinaryBuildMode.NewMediumCustomCompanion : paid ? Chunk7OrdinaryBuildMode.NewSmallCustomCompanion : Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                medium ? selection.MediumBody.RaceGuid : selection.SmallRaceGuid,
                medium ? selection.MediumBody.PresetGuid : selection.SmallPresetGuid,
                medium ? selection.MediumBody.SkeletonName : selection.SkeletonName,
                (medium ? selection.MediumBody.Gender : selection.Gender) == "Female" ? Gender.Female : Gender.Male,
                (medium ? selection.MediumBody.LeftHanded : selection.LeftHanded) == true,
                Chunk7ManualDerivedSelection.RangerAcquisitionLevel, medium ? Size.Medium : Size.Small);
            driver = new Chunk7OrdinaryBuildDriver(pins, paid ? null : actors.Single(actor => actor.UniqueId == selection.ExistingActorId),
                horses.HorseFeature, horses.HorseUnit.AssetGuid, AssertChoiceAuthority, ObserveChoice);
            ordinaryProbe = new NativeOrdinaryAcquisitionProbe(() => driver.Actor); ordinaryProbe.Install();
            // This family explicitly owns the existing one-slot diagnostic projection.
            // It does not overwrite the native settings or acquire additional writes.
            slotProjectionOwned = true; NativePersistenceIsolation.EnableNativeSlotRotation();
            evidence["choices"] = choices; evidence["beforeChoices"] = CaptureNoArrangement();
            evidence["originalPartyCharacters"] = new JArray(originalPartyCharacters);
            evidence["slotProjection"] = new JObject { ["initiallyEnabled"] = false, ["enabledForTerminalAuto"] = true, ["maximumNativeWrites"] = 1 };
            evidence["vendorDialogueProven"] = false; evidence["mountedCombatProven"] = false; evidence["smallPoseQualified"] = false;
            buildStarted = true; buildStage = 0; buildStageStart = clock.Elapsed.TotalSeconds; game.IsPaused = false;
            Pass("C7-manual-derived-eligibility");
        }
        private void AssertChoiceAuthority()
        {
            RequireAuthority(false); RequireNoOwners(); RequireNoEffects();
            Require(!cleanup && !Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) &&
                slotProjectionOwned && NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Native writer, unexpected UI or lost projection/world blocks ordinary input.");
        }
        private void ObserveChoice(JObject observation)
        { Require(choices.Count < 1400, "Native choice evidence exceeds its declared bound."); choices.Add(observation.DeepClone()); }
        private void TickBuild()
        {
            AssertChoiceAuthority();
            Require(clock.Elapsed.TotalSeconds - buildStageStart <= (buildStage == 0 ? 600 : 90), "Manual acquisition stage timed out: " + buildStage);
            if (buildStage == 0)
            {
                if (!buildInputAttempted)
                {
                    RequireLiveEligibility();
                    Require(game.CurrentMode == GameModeType.Default, "Initial native choice left the idle admitted mode.");
                    buildInputAttempted = true; // one attempted entry; thrown native input proceeds only to cleanup
                }
                if (driver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                RequireAcquiredActor(); evidence["builderComplete"] = driver.CaptureState();
                Pass("C7-manual-derived-native-choices"); buildStage = 1; buildStageStart = clock.Elapsed.TotalSeconds; return;
            }
            if (buildStage == 1)
            {
                // NOT READY - stale level-up autosave wait (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
                // The installed game never registers LevelUpAutoSaveController (GameModesFactory builds its
                // controllers explicitly without it), so an ordinary level-up never requests this native Auto
                // and the wait cannot complete. scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1 refuses this
                // family before any lock, transaction, Mods swap or launch. Kept intact until its completion
                // contract is repaired; never manufacture a level-up callback or relabel a Manual as Auto.
                if (!ObserveTerminalAutosave()) return;
                Pass("C7-manual-derived-native-autosave");
                if (selection.PaidHire)
                {
                    fieldAdmission = new Chunk7NativePartyFieldAdmission(driver.Actor, originalPartyCharacters, AssertFieldAuthority);
                    fieldAdmission.Open(); buildStage = 2; buildStageStart = clock.Elapsed.TotalSeconds; return;
                }
                buildStage = 3;
            }
            if (buildStage == 2)
            {
                if (!fieldAdmission.Tick()) return;
                evidence["fieldSelection"] = fieldAdmission.Capture(); buildStage = 3; buildStageStart = clock.Elapsed.TotalSeconds;
            }
            if (buildStage == 3 && CaptureField())
            {
                acquisitionProven = true; evidence["ordinaryAcquisitionProven"] = true;
                Pass("C7-manual-derived-native-field"); BeginCleanup();
            }
        }
        // NOT READY - stale level-up autosave wait (docs/CHUNK7-STALE-LEVELUP-AUTOSAVE-FAMILIES.md).
        // The installed game never registers LevelUpAutoSaveController (GameModesFactory builds its
        // controllers explicitly without it), so an ordinary level-up never requests this native Auto
        // and the wait cannot complete. scripts/runtime/Invoke-KingmakerRuntimeScenario.ps1 refuses this
        // family before any lock, transaction, Mods swap or launch. Kept intact until its completion
        // contract is repaired; never manufacture a level-up callback or relabel a Manual as Auto.
        private bool ObserveTerminalAutosave()
        {
            if (Busy() || ordinaryProbe.ObservedSave == null || ordinaryProbe.ObservedSave.OperationState != SaveInfo.StateType.None) return false;
            var save = ordinaryProbe.ObservedSave; var completion = persistence.LastCompletedSave;
            Require(ordinaryProbe.Complete && persistence.CompletedSaveCount == 1 && persistence.SnapshotCount == 1 &&
                authorization.AuthorizedWriteCount == 1 && authority.AdmittedNativeWriteRequests == 1 &&
                completion != null && completion.Sequence == 1 && ReferenceEquals(completion.Requested, ordinaryProbe.ObservedRequestedSave) &&
                ReferenceEquals(completion.Written, save) && completion.Path == Path.Combine(root, Chunk7ManualDerivedSelection.OutputLeaf) &&
                save.FileName == Chunk7ManualDerivedSelection.OutputLeaf && save.Type == SaveInfo.SaveType.Auto && ordinaryProbe.ObservedRequestedSave.IsAutoLevelupSave && !save.IsAutoLevelupSave &&
                save.GameId == request.PersistenceLoad.GameId && save.GameName == request.PersistenceLoad.GameName &&
                save.Area.AssetGuidThreadSafe == request.PersistenceLoad.Area && save.HasFileOnDisk && save.IsActuallySaved,
                "Native terminal autosave lacks exact causal, completion, descriptor or authority proof.");
            NativePersistenceIsolation.RequireOwnedWrite(save); VerifySourceBytes(); RequireNoOwners(); RequireNoEffects();
            var file = new FileInfo(save.FolderName); var archive = NativeMountedSaveStorage.ReadArchive(save.Saver);
            Require(file.Exists && file.FullName == Path.Combine(root, Chunk7ManualDerivedSelection.OutputLeaf) &&
                archive.Kind == MountedSaveReadKind.Current && archive.SourceSchema == 3 && archive.Data != null &&
                archive.Data.CampaignId == request.PersistenceLoad.GameId && archive.Data.AreaId == request.PersistenceLoad.Area &&
                archive.Data.Pairs.Length == 0 && archive.Data.Participations.Length == 0 && archive.Data.Slots.Length == 0 && archive.Data.Combat == null,
                "Ordinary native autosave contains unsupported or invented mounted restoration state.");
            var saves = manager.ToArray();
            Require(saves.Length == 2 && saves.Count(value => value.FileName == request.PersistenceLoad.FileName) == 1 &&
                saves.Count(value => ReferenceEquals(value, save)) == 1, "Native descriptor inventory differs from the one immutable Manual plus one terminal Auto.");
            evidence["nativeWrite"] = new JObject { ["path"] = file.FullName, ["leaf"] = save.FileName, ["length"] = file.Length,
                ["sha256"] = Hash(file.FullName), ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks, ["name"] = save.Name,
                ["requestedObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Requested), ["writtenObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Written),
                ["completionSequence"] = completion.Sequence, ["completedWritesBefore"] = 0, ["type"] = save.Type.ToString(), ["autoLevelup"] = save.IsAutoLevelupSave, ["operation"] = save.OperationState.ToString(),
                ["gameId"] = save.GameId, ["gameName"] = save.GameName, ["area"] = save.Area.AssetGuidThreadSafe,
                ["authorizedWrites"] = authorization.AuthorizedWriteCount, ["admittedWrites"] = authority.AdmittedNativeWriteRequests,
                ["completedWrites"] = persistence.CompletedSaveCount, ["snapshotCount"] = persistence.SnapshotCount,
                ["nativeCallbackSupplied"] = false, ["saveBeforeFieldSelection"] = selection.PaidHire,
                ["schema"] = archive.SourceSchema, ["emptyMountedArchive"] = true, ["descriptorCount"] = saves.Length };
            return true;
        }
        private void RequireAcquiredActor()
        {
            var actor = driver?.Actor; if (actor == null) { Require(acquiredActor == null, "Acquired actor disappeared from its native builder."); return; }
            if (acquiredActor == null) { acquiredActor = actor; acquiredDescriptor = actor.Descriptor; }
            Require(ReferenceEquals(actor, acquiredActor) && ReferenceEquals(actor.Descriptor, acquiredDescriptor), "Acquired native actor/descriptor was replaced.");
            if (actor.IsInState && actor.IsInGame && actor.View != null)
            {
                if (acquiredView == null) acquiredView = actor.View;
                Require(ReferenceEquals(acquiredView, actor.View), "Acquired field actor view was replaced.");
            }
            var pet = actor.Descriptor.Pet;
            if (pet == null) { Require(acquiredPet == null, "Acquired Horse disappeared."); return; }
            Require(pet.Blueprint == horses.HorseUnit && ReferenceEquals(pet.Descriptor.Master.Value, actor) &&
                actor.Descriptor.GetFact(horses.HorseFeature) is Feature fact && fact.Source != null,
                "Native acquired Horse lacks exact sourced feature and reciprocal master.");
            if (acquiredPet == null) { acquiredPet = pet; acquiredPetDescriptor = pet.Descriptor; }
            Require(ReferenceEquals(acquiredPet, pet) && ReferenceEquals(acquiredPetDescriptor, pet.Descriptor), "Native acquired Horse/descriptor was replaced.");
            if (pet.IsInState && pet.IsInGame && pet.View != null)
            {
                if (acquiredPetView == null) acquiredPetView = pet.View;
                Require(ReferenceEquals(acquiredPetView, pet.View), "Acquired Horse field view was replaced.");
            }
        }
        private bool CaptureField()
        {
            RequireAuthority(false); RequireNoOwners(); RequireNoEffects(); RequireAcquiredActor();
            var actor = acquiredActor; var pet = acquiredPet;
            if (actor?.View?.AgentASP == null || pet?.View?.AgentASP == null || !actor.IsInState || !actor.IsInGame || !pet.IsInState || !pet.IsInGame) return false;
            Require(actor.IsDirectlyControllable && pet.IsDirectlyControllable && actor.Commands.Empty && pet.Commands.Empty &&
                ReferenceEquals(pet.Descriptor.Master.Value, actor) && actor.Descriptor.GetFact(horses.HorseFeature) is Feature fact && fact.Source != null &&
                (actor.Descriptor.GetFact(horses.LevelRank)?.GetRank() ?? 0) > 0 && actor.GetActivePolymorph() == null &&
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1 &&
                originalPartyCharacters.All(id => game.Player.PartyCharacters.Count(value => value.Value?.UniqueId == id) == 1) &&
                game.Player.PartyCharacters.Count == originalPartyCharacters.Length + (selection.PaidHire ? 1 : 0),
                "Acquired owner/Horse lacks exact native field, party, source/rank or command settlement.");
            if (selection.PaidHire)
                Require(actor.Descriptor.OriginalSize == (selection.Case == Chunk7ManualDerivedSelection.PaidMediumCase ? Size.Medium : Size.Small) &&
                    actor.Descriptor.State.Size == actor.Descriptor.OriginalSize, "Selected paid body's original/effective size differs.");
            var body = NativeSmallRiderBody.Capture(actor, pet);
            if (selection.PaidHire && body?.NativeRigAvailable != true) return false;
            evidence["field"] = new JObject { ["frame"] = UnityEngine.Time.frameCount, ["actorId"] = actor.UniqueId,
                ["actorObject"] = probe.Identity(actor), ["actorDescriptor"] = probe.Identity(actor.Descriptor), ["actorView"] = probe.Identity(actor.View),
                ["petId"] = pet.UniqueId, ["petObject"] = probe.Identity(pet), ["petDescriptor"] = probe.Identity(pet.Descriptor), ["petView"] = probe.Identity(pet.View),
                ["horseBlueprint"] = pet.Blueprint.AssetGuid, ["featureBlueprint"] = horses.HorseFeature.AssetGuid,
                ["featureSourceGuid"] = ((Feature)actor.Descriptor.GetFact(horses.HorseFeature)).Source.AssetGuid,
                ["rank"] = actor.Descriptor.GetFact(horses.LevelRank).GetRank(), ["reciprocal"] = true,
                ["partyCharacters"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
                ["profileInventory"] = Chunk7NativeProfileInventory.Capture(new[] { actor, pet }),
                ["bodyObservation"] = JObject.FromObject(body ?? new SmallRiderBodyObservation()),
                ["ordinaryFieldAdmissionProven"] = true, ["mountedOrPoseAcceptanceProven"] = false };
            evidence["afterChoices"] = CaptureNoArrangement(); return true;
        }
        private void AssertCleanupInputAuthority()
        {
            RequireAuthority(true); RequireNoOwners();
            Require(!Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Native cleanup cannot cross an active writer/load/combat/foreign world boundary.");
        }
        private void AssertFieldAuthority()
        {
            if (!cleanup) { AssertChoiceAuthority(); return; }
            AssertCleanupInputAuthority();
            Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "Unexpected dialog prevents owned selector cleanup.");
        }
        private bool DrainBuildCustody()
        {
            if (driver != null && cancellation == null && driver.TryGetOwnedBuildSession(out var builder))
                cancellation = new Chunk7NativeBuilderCancellation(builder, AssertCleanupInputAuthority, Busy);
            if (cancellation != null && !cancellation.TryDrain()) return false;
            if (fieldAdmission != null && !fieldAdmission.TryDrain()) return false;
            return true;
        }
        private void RestoreAcquisitionPresentation()
        {
            if (!presentationCaptured) return;
            AssertCleanupInputAuthority();
            Require(DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown, "Unexpected native message box retains final presentation custody.");
            Require(ummInstance != null && ReferenceEquals(UnityModManagerNet.UnityModManager.UI.Instance, ummInstance),
                "Captured UMM instance was replaced; final presentation custody is retained.");
            if (slotProjectionOwned) { NativePersistenceIsolation.DisableNativeSlotRotation(); slotProjectionOwned = false; }
            SelectionManager.Instance.MultiSelect(originalSelection.Select(actor => actor.View), false);
            Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Exact original native selection was not restored.");
            ummLease?.Dispose();
            Require(ummInstance != null && ReferenceEquals(UnityModManagerNet.UnityModManager.UI.Instance, ummInstance) && ummInstance.Opened == originalUmmOpened,
                "Captured UMM open state was not restored; a disposed lease retry cannot prove restoration.");
            ummLease = null; game.IsPaused = originalPause;
            evidence["cleanup"]["ummInstanceRestored"] = true; evidence["cleanup"]["ummOpenStateRestored"] = true;
            evidence["cleanup"]["selectionRestored"] = true; evidence["cleanup"]["pauseRestored"] = game.IsPaused == originalPause;
            evidence["cleanup"]["slotProjectionRestored"] = !NativePersistenceIsolation.NativeSlotRotationEnabled;
            evidence["cleanup"]["acquiredNativeStateRetained"] = true;
            evidence["cleanup"]["manualRefunds"] = 0; evidence["cleanup"]["manualFactRemovals"] = 0; evidence["cleanup"]["manualCompanionRemovals"] = 0;
        }
    }
}
