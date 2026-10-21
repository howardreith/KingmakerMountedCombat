using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7AcquiredManualScenario
    {
        private readonly HorseCompanionBlueprintService horses;
        private JObject sourceObservations, noArrangementLoaded, resourceBefore;
        private UnitEntityData acquiredActor, acquiredPet;
        private object acquiredDescriptor, acquiredPetDescriptor, acquiredView, acquiredPetView, acquiredFeature, acquiredRank;
        private bool sourcePaid, sourcePaidMedium, ownerBound, fieldStarted, fieldTransitionOpen, fieldProven;
        private Chunk7NativePartyFieldAdmission fieldAdmission;
        private UnitEntityData[] originalSelection;
        private string[] originalCharacters;
        private bool presentationCaptured, presentationRestored, originalPause, originalUmmOpened;
        private MovementCaptureOverlayLease ummLease;
        private UnityModManagerNet.UnityModManager.UI ummInstance;

        private UnitEntityData[] BindColdActors()
        {
            var expected = ((JArray)(TwoOwners ? sourceWorld : sourceSavedWorld)["party"])
                .Select(row => new { Id = (string)row["id"], Blueprint = (string)row["blueprint"] }).ToList();
            if (TwoOwners)
            {
                Require(expected.All(row => row.Id != selection.PetId), "Cumulative original roster already aliases its newly acquired Horse.");
                if (!sourcePaid) expected.Add(new { Id = selection.PetId, Blueprint = (string)sourceObservations["field"]["horseBlueprint"] });
            }
            var current = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(current.Length == expected.Count && current.All(value => value != null) &&
                current.Select(value => value.UniqueId).Distinct(StringComparer.Ordinal).Count() == current.Length,
                "Cold acquisition roster differs from its exact saved pre-selector layout.");
            foreach (var value in current)
            {
                Require(expected.Count(row => row.Id == value.UniqueId && row.Blueprint == value.Blueprint?.AssetGuid) == 1,
                    "Cold native actor or blueprint was substituted.");
                if (!value.IsInState || !value.IsInGame || value.View?.AgentASP == null) return null;
                Require(value.Commands.Empty && !value.IsInCombat, "Cold field actor is unexpectedly busy or in combat.");
            }
            return current;
        }
        private UnitEntityData Exact(string id)
        {
            var found = game.State.Units.Concat(game.Player.AllCharacters).Where(value => value != null && value.UniqueId == id).Distinct().ToArray();
            Require(found.Length == 1, "Cold actor ID is absent or ambiguous in the exact native state/player graph."); return found[0];
        }
        private void BindAcquiredOwner()
        {
            Require(!ownerBound && horses.State == HorseCompanionBlueprintState.Registered && horses.HorseFeature != null &&
                horses.HorseUnit != null && horses.LevelRank != null, "Exact ordinary Horse registration is unavailable.");
            acquiredActor = Exact(selection.ActorId); acquiredPet = acquiredActor.Descriptor.Pet;
            Require(acquiredPet != null && acquiredPet.UniqueId == selection.PetId && acquiredPet.Blueprint == horses.HorseUnit &&
                ReferenceEquals(acquiredPet.Descriptor.Master.Value, acquiredActor), "Native cold load did not restore the exact reciprocal sourced owner/Horse.");
            acquiredDescriptor = acquiredActor.Descriptor; acquiredPetDescriptor = acquiredPet.Descriptor;
            acquiredView = acquiredActor.View; acquiredPetView = acquiredPet.View;
            acquiredFeature = acquiredActor.Descriptor.GetFact(horses.HorseFeature); acquiredRank = acquiredActor.Descriptor.GetFact(horses.LevelRank);
            originalCharacters = game.Player.PartyCharacters.Select(value => value.Value.UniqueId).ToArray();
            Require(originalCharacters.Length > 0 && originalCharacters.Length <= 6 && originalCharacters.Distinct(StringComparer.Ordinal).Count() == originalCharacters.Length,
                "Cold native field character roster is unbounded or duplicate.");
            var fieldMembers = game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, acquiredActor));
            var remoteMembers = game.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, acquiredActor));
            Require(TwoOwners && sourcePaid ? fieldMembers == 0 && remoteMembers == 1 && !acquiredActor.IsInGame && originalCharacters.Length < 6 :
                fieldMembers == 1 && remoteMembers == 0 && actors.Any(value => ReferenceEquals(value, acquiredActor)) && actors.Any(value => ReferenceEquals(value, acquiredPet)),
                "Cold owner membership differs from the actual source native save/selector order.");
            ownerBound = true; RequireAcquiredOwner();
            var originals = ((JArray)sourceObservations["afterChoices"]["actors"]).Select(row => (string)row["actorId"]).OrderBy(value => value, StringComparer.Ordinal).ToArray();
            Require(originals.SequenceEqual(((JArray)sourceWorld["party"]).Select(row => (string)row["id"]).OrderBy(value => value, StringComparer.Ordinal)),
                "Source acquired progression receipt does not identify its exact original roster.");
            var actualOriginals = CaptureNoArrangement();
            actualOriginals["actors"] = new JArray(((JArray)actualOriginals["actors"]).Where(row => originals.Contains((string)row["actorId"])).Select(row => row.DeepClone()));
            Require(JToken.DeepEquals(actualOriginals, sourceObservations["afterChoices"]), "Cold original class/XP/money state differs from the completed acquisition.");
            if (!TwoOwners)
            {
                var savedAcquired = (JArray)sourceObservations["afterSaveArrangement"]["acquired"];
                var coldAcquired = new JArray(((JArray)CaptureNoArrangement()["actors"])
                    .Where(row => (string)row["actorId"] == selection.ActorId || (string)row["actorId"] == selection.PetId)
                    .Select(row => row.DeepClone()));
                Require(JToken.DeepEquals(coldAcquired, savedAcquired),
                    "Cold owner/Horse XP, level or class ranks differ from the exact saved field Manual.");
            }
            resourceBefore = CaptureResources(); evidence["resourcesBeforeField"] = resourceBefore.DeepClone();
        }
        private UnitEntityData[] OwnedActors() => (actors ?? new UnitEntityData[0]).Concat(new[] { acquiredActor, acquiredPet })
            .Where(value => value != null).Distinct().OrderBy(value => value.UniqueId, StringComparer.Ordinal).ToArray();
        private void RequireAcquiredOwner()
        {
            Require(acquiredActor != null && acquiredPet != null && acquiredActor.UniqueId == selection.ActorId && acquiredPet.UniqueId == selection.PetId &&
                ReferenceEquals(acquiredActor.Descriptor, acquiredDescriptor) && ReferenceEquals(acquiredPet.Descriptor, acquiredPetDescriptor) &&
                ReferenceEquals(acquiredActor.Descriptor.Pet, acquiredPet) && ReferenceEquals(acquiredPet.Descriptor.Master.Value, acquiredActor) &&
                acquiredPet.Blueprint == horses.HorseUnit && acquiredActor.GetActivePolymorph() == null && acquiredPet.GetActivePolymorph() == null &&
                acquiredActor.Descriptor.State.IsConscious && !acquiredActor.Descriptor.State.IsDead && !acquiredPet.Descriptor.State.IsDead,
                "Admitted acquired owner/pet identity, reciprocal graph or living body changed.");
            if (acquiredView == null && acquiredActor.View != null && fieldTransitionOpen) acquiredView = acquiredActor.View;
            if (acquiredPetView == null && acquiredPet.View != null && fieldTransitionOpen) acquiredPetView = acquiredPet.View;
            Require(ReferenceEquals(acquiredView, acquiredActor.View) && ReferenceEquals(acquiredPetView, acquiredPet.View),
                "Acquired owner/Horse view changed outside its one owned native field materialization.");
            var fact = acquiredActor.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(fact != null && ReferenceEquals(fact, acquiredFeature) && fact.Source != null &&
                fact.Source.AssetGuid == (string)sourceObservations["field"]["featureSourceGuid"] &&
                horses.HorseFeature.AssetGuid == (string)sourceObservations["field"]["featureBlueprint"] &&
                ReferenceEquals(acquiredActor.Descriptor.GetFact(horses.LevelRank), acquiredRank) &&
                acquiredActor.Descriptor.GetFact(horses.LevelRank).GetRank() == (int)sourceObservations["field"]["rank"],
                "Acquired Horse no longer has its exact sourced feature and native rank.");
            var prior = sourceObservations["builderComplete"];
            Require(acquiredActor.Descriptor.Progression.CharacterLevel == (int)prior["level"] && acquiredActor.Descriptor.Progression.Experience == (int)prior["xp"] &&
                acquiredPet.Descriptor.Progression.CharacterLevel == (int)prior["petLevel"] && acquiredPet.Descriptor.Progression.Experience == (int)prior["petExperience"],
                "Cold acquired progression differs from the exact completed native builder.");
            var classRows = acquiredActor.Descriptor.Progression.Classes.ToArray();
            Require(classRows.Length > 0 && classRows.Length <= 20 && classRows.All(value => value?.CharacterClass != null && value.Level > 0 && value.Level <= 20) &&
                classRows.Sum(value => value.Level) == acquiredActor.Descriptor.Progression.CharacterLevel &&
                classRows.Count(value => value.CharacterClass.AssetGuid == "cda0615668a6df14eb36ba19ee881af6" && value.Level == (int)prior["rangerLevel"]) == 1,
                "Cold acquired Ranger class levels differ from the native completion.");
            if (sourcePaid) Require(classRows.Length == 1 && (int)prior["rangerLevel"] == (int)prior["level"],
                "Paid native acquisition contains an unobserved class.");
        }
        private JObject CaptureAcquiredOwner()
        {
            RequireAcquiredOwner(); return CaptureOwner(acquiredActor, acquiredPet, (Feature)acquiredFeature, acquiredRank);
        }
        private JObject CaptureOwner(UnitEntityData owner, UnitEntityData pet, Feature feature, object rank)
        {
            return new JObject { ["frame"] = UnityEngine.Time.frameCount, ["actorId"] = owner.UniqueId, ["petId"] = pet.UniqueId,
                ["actorObject"] = probe.Identity(owner), ["actorDescriptor"] = probe.Identity(owner.Descriptor), ["actorView"] = probe.Identity(owner.View),
                ["petObject"] = probe.Identity(pet), ["petDescriptor"] = probe.Identity(pet.Descriptor), ["petView"] = probe.Identity(pet.View),
                ["actorBlueprint"] = owner.Blueprint.AssetGuid, ["petBlueprint"] = pet.Blueprint.AssetGuid,
                ["featureObject"] = probe.Identity(feature), ["rankObject"] = probe.Identity(rank),
                ["featureGuid"] = horses.HorseFeature.AssetGuid, ["sourceGuid"] = feature.Source.AssetGuid, ["rank"] = owner.Descriptor.GetFact(horses.LevelRank).GetRank(),
                ["reciprocal"] = true, ["actorInGame"] = owner.IsInGame, ["petInGame"] = pet.IsInGame,
                ["actorInState"] = owner.IsInState, ["petInState"] = pet.IsInState,
                ["actorAgentReady"] = owner.View?.AgentASP != null, ["petAgentReady"] = pet.View?.AgentASP != null,
                ["actorCommandsEmpty"] = owner.Commands.Empty, ["petCommandsEmpty"] = pet.Commands.Empty,
                ["partyCharacters"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
                ["remoteCompanions"] = new JArray(game.Player.RemoteCompanions.Select(value => value.Value.UniqueId)),
                ["progression"] = CaptureNoArrangement() };
        }
        private JObject CaptureResources() => new JObject { ["actors"] = new JArray(OwnedActors().Select(value => new JObject {
            ["actorId"] = value.UniqueId, ["move"] = value.CombatState.Cooldown.MoveAction, ["standard"] = value.CombatState.Cooldown.StandardAction,
            ["swift"] = value.CombatState.Cooldown.SwiftAction, ["prepared"] = value.CombatState.Prepared,
            ["reactions"] = value.CombatState.AttackOfOpportunityCount, ["reactionAllowance"] = value.CombatState.AttackOfOpportunityPerRound,
            ["initiative"] = value.CombatState.Initiative })) };
        private void StartField()
        {
            AssertFieldAuthority(); Require(!fieldStarted, "Native field admission may begin once."); fieldStarted = true;
            evidence["sourceSavedBeforeSelector"] = TwoOwners && sourcePaid;
            if (!TwoOwners || !sourcePaid) { evidence["selector"] = new JObject { ["required"] = false, ["openCount"] = 0, ["manualPartyMutation"] = false }; return; }
            Require(game.UI?.CharacterBuildController != null && game.UI.CharacterBuildController.LevelUpController == null && game.UI.CharacterBuildController.Unit == null,
                "A pre-existing character builder prevents native field input.");
            originalSelection = SelectionManager.Instance.SelectedUnits.ToArray();
            Require(originalSelection.All(value => actors.Any(actor => ReferenceEquals(actor, value))) && originalSelection.Distinct().Count() == originalSelection.Length,
                "Original selection includes a foreign actor.");
            originalPause = game.IsPaused; ummInstance = UnityModManagerNet.UnityModManager.UI.Instance;
            Require(ummInstance != null && ummLease == null, "Exact unowned UMM presentation is unavailable."); originalUmmOpened = ummInstance.Opened;
            presentationCaptured = true; MovementScreenshotCaptureCoordinator.AcquireClosedUmmLeaseRetained(out ummLease);
            fieldAdmission = new Chunk7NativePartyFieldAdmission(acquiredActor, originalCharacters, AssertFieldAuthority);
            fieldTransitionOpen = true; game.IsPaused = false; fieldAdmission.Open();
        }
        private void TickField()
        {
            AssertFieldAuthority(); Require(fieldStarted, "Native field phase did not start.");
            if (fieldAdmission != null && !fieldAdmission.Completed && !fieldAdmission.Tick()) return;
            if (acquiredActor.View?.AgentASP == null || acquiredPet.View?.AgentASP == null || !acquiredActor.IsInGame || !acquiredPet.IsInGame ||
                !acquiredActor.IsInState || !acquiredPet.IsInState) return;
            RequireAcquiredOwner(); var current = Chunk7NativeFieldRoster.Capture(game.Player); var expected = OwnedActors();
            Require(current.Length == expected.Length && current.All(value => expected.Count(actor => ReferenceEquals(actor, value)) == 1) &&
                expected.All(value => value.Commands.Empty && !value.IsInCombat && value.IsDirectlyControllable) &&
                game.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, acquiredActor)) == 1 &&
                game.Player.PartyCharacters.Count == originalCharacters.Length + (TwoOwners && sourcePaid ? 1 : 0) &&
                originalCharacters.All(id => game.Player.PartyCharacters.Count(value => value.Value?.UniqueId == id) == 1) &&
                !game.Player.RemoteCompanions.Any(value => ReferenceEquals(value.Value, acquiredActor)),
                "Native field placement changed original membership or left the acquired pair unsettled.");
            actors = current; descriptors = actors.Select(value => (object)value.Descriptor).ToArray(); views = actors.Select(value => (object)value.View).ToArray();
            fieldTransitionOpen = false; RequireWorld();
            Require(JToken.DeepEquals(noArrangementLoaded, CaptureNoArrangement()), "Native field placement changed XP/money/class state.");
            Require(JToken.DeepEquals(resourceBefore, CaptureResources()), "Native field placement changed action/reaction resources.");
            if (sourcePaid)
            {
                var body = NativeSmallRiderBody.Capture(acquiredActor, acquiredPet);
                if (body?.NativeRigAvailable != true) return;
                var pins = sourcePaidMedium ? sourceObservations["selection"]["mediumBody"] : sourceObservations["selection"];
                var expectedSize = sourcePaidMedium ? Size.Medium : Size.Small;
                Require(body.NativeRaceAndPresetIdentityExact && body.NativeSkeletonIsExactPreset && !body.RiderPolymorphed && !body.MountPolymorphed &&
                    body.OriginalSizeOrdinal == (int)expectedSize && body.EffectiveSizeOrdinal == (int)expectedSize && body.RaceGuid == (string)pins[sourcePaidMedium ? "raceGuid" : "smallRaceGuid"] &&
                    body.PresetGuid == (string)pins[sourcePaidMedium ? "presetGuid" : "smallPresetGuid"] && body.SkeletonName == (string)pins["skeletonName"] && body.Gender == (string)pins["gender"] &&
                    body.LeftHanded == (bool)pins["leftHanded"], "Native restored paid body differs from its explicit acquired source.");
                evidence["bodyObservation"] = JObject.FromObject(body);
            }
            if (fieldAdmission != null) { evidence["selector"] = fieldAdmission.Capture(); evidence["selector"]["required"] = true; }
            evidence["fieldOwner"] = CaptureAcquiredOwner(); evidence["resourcesAfterField"] = CaptureResources();
            CaptureTwoOwners("fieldTwoOwners");
            RestoreFieldPresentation(); evidence["fieldWorld"] = Snapshot();
            evidence["nativeProfileInventory"] = Chunk7NativeProfileInventory.Capture(actors);
            Require((string)evidence["nativeProfileInventory"]["captureStatus"] == "PASS", "Restored cumulative native profile inventory is unavailable.");
            evidence["fixtureReadiness"] = CaptureFixtureReadiness();
            Require(evidence["fixtureReadiness"]["saveSettings"]?["onlyOneSave"]?.Type == JTokenType.Boolean &&
                !(bool)evidence["fixtureReadiness"]["saveSettings"]["onlyOneSave"], "Fresh Manual derivative refuses one-save mode.");
            fieldProven = true; Pass("C7-acquired-manual-native-field"); BeginManualSave();
        }
        private void AssertFieldAuthority()
        {
            RequireAuthority(cleanup); RequireNoOwners(); RequireNoEffects();
            Require(!Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown) && !NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Native writer/load/combat/foreign dialog or slot projection retains field input custody.");
        }
        private void RestoreFieldPresentation()
        {
            if (!presentationCaptured || presentationRestored) return;
            AssertFieldAuthority();
            Require(fieldAdmission == null || !fieldAdmission.Owned, "Owned selector must settle before presentation restoration.");
            Require(fieldAdmission == null || fieldAdmission.TryDrain(), "Completed native selector changed before presentation restoration.");
            Require(ummInstance != null && ReferenceEquals(UnityModManagerNet.UnityModManager.UI.Instance, ummInstance),
                "Captured UMM instance changed; presentation custody is retained.");
            SelectionManager.Instance.MultiSelect(originalSelection.Select(value => value.View), false);
            Require(SelectionManager.Instance.SelectedUnits.SequenceEqual(originalSelection), "Original native selection did not restore exactly.");
            ummLease?.Dispose();
            Require(ReferenceEquals(UnityModManagerNet.UnityModManager.UI.Instance, ummInstance) && ummInstance.Opened == originalUmmOpened,
                "Exact UMM open state did not restore; custody remains retained.");
            ummLease = null; game.IsPaused = originalPause; presentationRestored = true;
            evidence["presentation"] = new JObject { ["selectionRestored"] = true, ["ummInstanceRestored"] = true, ["ummOpenedRestored"] = true,
                ["pauseRestored"] = game.IsPaused == originalPause, ["slotProjectionEnabled"] = NativePersistenceIsolation.NativeSlotRotationEnabled };
        }
        private JObject ReservationSnapshot()
        {
            Require(authority.AcquiredManualReservationCount == 1 && authority.AcquiredManualSourceDescriptor is SaveInfo descriptor &&
                manager.Count() == 1 && manager.Any(value => ReferenceEquals(value, descriptor)), "Exact pre-load native reservation descriptor was replaced.");
            return new JObject { ["frame"] = UnityEngine.Time.frameCount, ["sourceDescriptor"] = probe.Identity(authority.AcquiredManualSourceDescriptor),
                ["number"] = authority.AcquiredManualNumber, ["outputLeaf"] = authority.AcquiredManualOutputLeaf,
                ["count"] = authority.AcquiredManualReservationCount, ["beforeLoad"] = authority.AdmittedNativeLoadRequests == 0,
                ["initialEntries"] = authority.AcquiredManualInitialEntries, ["reservedEntries"] = authority.AcquiredManualReservedEntries,
                ["initialWritableEntries"] = authority.AcquiredManualInitialWritable, ["reservedWritableEntries"] = authority.AcquiredManualReservedWritable,
                ["sourceDescriptors"] = new JArray(manager.Select(value => new JObject { ["descriptor"] = probe.Identity(value),
                    ["fileName"] = value.FileName, ["internalName"] = value.Name, ["type"] = value.Type.ToString(),
                    ["gameId"] = value.GameId, ["gameName"] = value.GameName, ["area"] = value.Area?.AssetGuidThreadSafe,
                    ["isActuallySaved"] = value.IsActuallySaved, ["hasFileOnDisk"] = value.HasFileOnDisk,
                    ["operationState"] = value.OperationState.ToString(), ["autoLevelup"] = value.IsAutoLevelupSave })),
                ["slotProjectionEnabled"] = NativePersistenceIsolation.NativeSlotRotationEnabled,
                ["nativeMethod"] = "FindUnusedSaveNumber", ["nativeMethodToken"] = "06008024",
                ["nativeMethodMvid"] = typeof(SaveManager).Module.ModuleVersionId.ToString("D"), ["authority"] = AuthoritySnapshot() };
        }
    }
}
