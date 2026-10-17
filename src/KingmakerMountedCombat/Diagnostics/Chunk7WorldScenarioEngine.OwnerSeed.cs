using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Classes;
using Kingmaker.Blueprints.Root;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.EntitySystem.Persistence;
using Kingmaker.Enums;
using Kingmaker.UI.SettingsUI;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using Kingmaker.UnitLogic.Parts;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Owner-seed two-pair exploration on a disposable run copy of an owner-supplied
    // seed. SETUP is disclosed native fixture setup: the ordinary Ranger build driver
    // for each rider (Hunter's Bond -> Ranger companion -> KMC Horse) and, for rider
    // B only, the exact native XP its eligibility requires. SETUP-COMPLETE then closes
    // every setup mutation; MEASURED reuses the unchanged two-pair steps. The created
    // Horses are fixture setup, never acquisition proof. The only native writes are the
    // game's own level-up Autos during SETUP, reserved one at a time in the run root
    // (the assisted fixture's reviewed level-up precedent); none follow SETUP-COMPLETE.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string OwnerSeedSetupRow = "C7-world-owner-seed-setup";
        internal const string OwnerSeedDisclosure = "Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter's Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof; the only native writes are the game's own level-up Autos into the run-owned isolated save root during setup, none after SETUP-COMPLETE.";
        private const int OwnerSeedEventLimit = 768;
        private const double OwnerSeedSetupStepSeconds = 240, OwnerSeedSetupTotalSeconds = 420;
        private Chunk7OwnerSeedSetupLedger setupLedger;
        private Chunk7OrdinaryBuildDriver setupDriver;
        private readonly Stopwatch setupClock = new Stopwatch();
        private JObject ownerSeedSetup, setupBoundaryState;
        private JArray setupRiderRows, setupEvents;
        private int setupEventCount;
        private bool OwnerSeed => setupLedger != null;
        // Native level-up autosave custody (the assisted fixture precedent): an owned slot
        // projection while the builder is active, one reservation and probe per build.
        private NativeOrdinaryAcquisitionProbe setupProbe;
        private bool setupSlotOwned;
        private int setupWritesExpected = -1, setupWritesAtBoundary = -1;
        private JArray setupAutosaves;
        private JObject setupEconomyBefore;
        private UnitEntityData[] setupCharacters;

        // A refusal before SETUP-COMPLETE is a setup failure, not a behaviour result.
        private void MarkOwnerSeedSetupFailure()
        {
            if (request?.OwnedPairs?.IsOwnerSeed == true && evidence["failurePhase"] == null)
                evidence["failurePhase"] = setupLedger?.Completed == true ? "MEASURED" : "SETUP";
        }

        private void BindOwnerSeed(Game game)
        {
            var selection = request.OwnedPairs;
            setupClock.Start();
            RequireOwnerSeedAuthority();
            Require(horses.State == HorseCompanionBlueprintState.Registered && horses.HorseFeature != null && horses.LevelRank != null && horses.HorseUnit != null,
                "Exact registered ordinary Horse contracts are unavailable.");
            var input = request.PersistenceLoad;
            Require(game.Player.MainCharacter.Value?.CharacterName == input.GameName && game.CurrentlyLoadedArea?.AssetGuidThreadSafe == input.Area &&
                persistence != null && persistence.LoadedArchive == null && persistence.LoadedArchiveKind == MountedSaveReadKind.Missing && !persistence.LoadedArchiveCanRestore,
                "The owner seed must load through the supported missing-metadata path (it proves no mounted restoration).");
            originalParty = Chunk7NativeFieldRoster.Capture(game.Player);
            Require(originalParty.Select(x => x.UniqueId).OrderBy(x => x, StringComparer.Ordinal).SequenceEqual(selection.PartyIds.OrderBy(x => x, StringComparer.Ordinal)),
                "Loaded native party differs from the selected owner-seed party. No substitute actor is selected.");
            firstRider = Exact(selection.RiderAId); secondRider = Exact(selection.RiderBId);
            evidence["partyBefore"] = new JArray(originalParty.Select(CaptureRig));
            setupLedger = new Chunk7OwnerSeedSetupLedger(); setupRiderRows = new JArray(); setupEvents = new JArray();
            // Money and the party item inventory before any mutation; required equal at the boundary.
            setupCharacters = game.Player.PartyCharacters.Select(value => value.Value).ToArray();
            Require(setupCharacters.Length >= 1 && setupCharacters.All(actor => actor != null && originalParty.Contains(actor)), "Owner-seed party characters are unavailable.");
            setupEconomyBefore = SetupEconomy();
            var a = RiderFacts(firstRider); var b = RiderFacts(secondRider);
            var settings = SettingsRoot.Instance;
            Require(settings?.AutosaveEnabled != null && settings.OnlyOneSave != null && settings.AutosaveSlots != null, "Native save settings are unavailable.");
            // Read-only, before any projection exists: the actual native setting decides the case.
            Require(!NativePersistenceIsolation.NativeSlotRotationEnabled, "A pre-existing slot projection would hide the native autosave settings.");
            var autosaveEnabled = settings.AutosaveEnabled.CurrentValue; var onlyOneSave = settings.OnlyOneSave.CurrentValue;
            var autosaveSlots = settings.AutosaveSlots.CurrentValue;
            var riderBuilds = new[] { firstRider, secondRider }.Count(rider => rider.Descriptor.Pet == null);
            setupWritesExpected = Chunk7OwnerSeedSetupPlan.ExpectedSetupWrites(autosaveEnabled, riderBuilds); setupAutosaves = new JArray();
            ownerSeedSetup = new JObject { ["contract"] = "owner-seed-fixture-setup-v1", ["disclosure"] = OwnerSeedDisclosure,
                ["seedId"] = selection.SeedId, ["seedSha256"] = selection.SeedSha256, ["fixtureSetupOnly"] = true, ["acquisitionProof"] = false,
                ["producerAncestry"] = false, ["loadedArchiveKind"] = "Missing", ["loadedArchiveCanRestore"] = false,
                ["riderFacts"] = new JArray(FactsRow("A", a), FactsRow("B", b)),
                ["nativeLevelUpAutosave"] = new JObject { ["autosaveEnabled"] = autosaveEnabled, ["onlyOneSave"] = onlyOneSave,
                    ["autosaveSlots"] = autosaveSlots, ["settingsWritten"] = false, ["case"] = autosaveEnabled ? "autosave-on" : "autosave-off",
                    ["riderBuilds"] = riderBuilds, ["expectedWrites"] = setupWritesExpected, ["selectedWrites"] = selection.ExpectedNativeWriteCount,
                    ["slotProjectionDuringBuild"] = setupWritesExpected > 0, ["mountedPersistenceProof"] = false },
                ["riders"] = setupRiderRows, ["events"] = setupEvents, ["setupAutosaves"] = setupAutosaves,
                ["economyBefore"] = setupEconomyBefore, ["nativeXpTable"] = new JArray(Enumerable.Range(1, 20).Select(level => BlueprintRoot.Instance.Progression.XPTable.GetBonus(level))) };
            evidence["ownerSeedSetup"] = ownerSeedSetup;
            var refusals = Chunk7OwnerSeedSetupPlan.RiderErrors(a, "A").Concat(Chunk7OwnerSeedSetupPlan.RiderErrors(b, "B"))
                .Concat(Chunk7OwnerSeedSetupPlan.AssistanceErrors(selection, a, b))
                .Concat(Chunk7OwnerSeedSetupPlan.WriteErrors(selection, autosaveEnabled, onlyOneSave, riderBuilds)).ToArray();
            ownerSeedSetup["admissionRefusals"] = new JArray(refusals);
            Require(refusals.Length == 0, "Owner-seed SETUP refused before any mutation (setup failure, not a behaviour result): " + string.Join(" ", refusals));
            foreach (var rider in new[] { firstRider, secondRider })
            {
                string rigError;
                Require(MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, SupportedMountedProfiles.Horse.RiderPoseProfile, out rigError),
                    "Owner-seed rider rig refused before setup: " + rigError);
            }
            evidence["ownerSeed"] = new JObject { ["contract"] = "owner-seed-two-pair-exploration-v1", ["seedId"] = selection.SeedId,
                ["seedSha256"] = selection.SeedSha256, ["riderAId"] = selection.RiderAId, ["riderBId"] = selection.RiderBId,
                ["nativeLoadVerifiedByHost"] = true, ["fixtureSetupOnly"] = true, ["acquisitionProof"] = false };
        }

        private void RequireOwnerSeedAuthority()
        {
            var input = request.PersistenceLoad;
            Require(request.OwnedPairs?.IsOwnerSeed == true && request.Validate().Count == 0 && input != null &&
                fixtureAuthorization != null && fixtureAuthority != null && fixtureLoader != null && fixtureAuthorization.IsActive &&
                fixtureAuthorization.IsExactPersistenceScope(fixtureAuthority) && NativePersistenceIsolation.IsExactAuthority(fixtureAuthority) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, false) && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests <= fixtureAuthority.OwnerSeedConfirmedWrites + 1 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == fixtureAuthority.AdmittedNativeWriteRequests && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                fixtureAuthorization.UnauthorizedLoadCount == 0 &&
                Game.Instance?.SaveManager != null && Game.Instance.SaveManager.SavePath == fixtureAuthority.Root &&
                fixtureLoader.State == WorkingFixtureLoadState.LoadedAndVerified &&
                fixtureLoader.WorkingPath == Path.Combine(fixtureAuthority.Root, input.FileName) && fixtureLoader.Descriptor != null &&
                fixtureLoader.Descriptor.FileName == input.FileName && fixtureLoader.Descriptor.GameId == input.GameId &&
                fixtureLoader.Descriptor.Area?.AssetGuidThreadSafe == input.Area,
                "Owner-seed setup lost its exact one-load, reserved-level-up-only isolated authority.");
        }

        // Owner-seed SETUP. Returns after advancing; measured steps never enter here.
        private void UpdateOwnerSeedSetup()
        {
            Require(OwnerSeed && !setupLedger.Completed, "Owner-seed setup step outside the open SETUP phase.");
            Require(setupClock.Elapsed.TotalSeconds <= OwnerSeedSetupTotalSeconds && clock.Elapsed.TotalSeconds - stepStarted <= OwnerSeedSetupStepSeconds,
                "Bounded owner-seed setup deadline at step " + step + ".");
            switch (step)
            {
                case 50:
                case 52:
                    if (setupDriver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                    FinishOwnerSeedRider(step == 50 ? 0 : 1);
                    Next(step == 50 ? 56 : 57); break;
                case 56:
                case 57:
                    if (!ObserveSetupAutosave(step == 56 ? 0 : 1)) return;
                    Next(step == 56 ? 51 : 53); break;
                case 51:
                    ApplyOwnerSeedExperience(); BeginOwnerSeedRider(1); Next(52); break;
                case 53:
                    // New pets materialize after the native commit; wait (bounded) until all four actors are idle.
                    if (firstMount?.View?.AgentASP == null || !firstMount.IsInState || secondMount?.View?.AgentASP == null || !secondMount.IsInState ||
                        !Actors().All(Idle)) return;
                    CompleteOwnerSeedSetup();
                    evidence["partyWithHorse"] = new JArray(Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Select(CaptureRig));
                    BeginMeasurement("unmounted-0"); Next(20); break;
                default: throw new InvalidOperationException("Unknown owner-seed setup step " + step + ".");
            }
        }

        private void BeginOwnerSeedRider(int index)
        {
            setupLedger.RequireOpen("native ordinary Ranger build");
            RequireOwnerSeedAuthority();
            var rider = index == 0 ? firstRider : secondRider;
            setupRiderRows.Add(new JObject { ["role"] = index == 0 ? "A" : "B", ["riderId"] = rider.UniqueId, ["before"] = ProgressionState(rider) });
            var pins = new Chunk7OrdinaryBuildPins(request.RunId, request.OwnedPairs.SeedSha256, Chunk7OrdinaryBuildMode.ExistingRangerLevels,
                null, null, null, Gender.Male, false, Chunk7OwnerSeedSetupPlan.RequiredRangerLevel, Size.Medium, true);
            var game = Game.Instance;
            if (setupWritesExpected > 0)
            {
                Require(game.SaveManager.IsSaveAllowed() && !NativePersistenceIsolation.HasPendingWrites, "The native level-up Auto cannot be admitted in the current world state.");
                if (!setupSlotOwned)
                {
                    Require(!NativePersistenceIsolation.NativeSlotRotationEnabled, "A pre-existing slot projection cannot be borrowed.");
                    setupSlotOwned = true; NativePersistenceIsolation.EnableNativeSlotRotation();
                }
                fixtureAuthority.ReserveOwnerSeedLevelup(request, game, game.SaveManager, RuntimePersistenceScenario.SlotName(SaveInfo.SaveType.Auto));
            }
            Require(setupProbe == null, "A previous native level-up observer is still owned.");
            // The existing probe factories already pin the two reserved prepared leaves:
            // ordinary = Auto_1 (first setup Auto), assisted = Auto_2 (the staging copy).
            setupProbe = index == 0 ? new NativeOrdinaryAcquisitionProbe(() => rider) : NativeOrdinaryAcquisitionProbe.ForAssistedFixture(() => rider);
            setupProbe.Install();
            setupDriver = new Chunk7OrdinaryBuildDriver(pins, rider, horses.HorseFeature, horses.HorseUnit.AssetGuid, RequireOwnerSeedSetupWindow, PublishSetupEvent);
        }

        private bool SetupBusy() => LoadingProcess.Instance.IsLoadingInProcess || LoadingProcess.Instance.QueuedNames.Any() ||
            NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy;

        // After each build's native completion: exactly its one level-up Auto (autosave on)
        // completed at the reserved run-owned leaf, or its request natively suppressed (off).
        private bool ObserveSetupAutosave(int index)
        {
            if (SetupBusy()) return false;
            var rider = index == 0 ? firstRider : secondRider; var probe = setupProbe; var sequence = index + 1;
            RequireOwnerSeedAuthority();
            if (setupWritesExpected == 0)
            {
                var observed = probe?.Capture();
                Require(observed != null && (int)observed["saveRequests"] == 1 && (int)observed["preparedWrites"] == 0 && !NativePersistenceIsolation.NativeSlotRotationEnabled &&
                    fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedWriteCount == 0 && persistence.CompletedSaveCount == 0,
                    "With native autosave off the level-up request must write nothing.");
                setupAutosaves.Add(new JObject { ["sequence"] = sequence, ["riderId"] = rider.UniqueId, ["case"] = "autosave-off",
                    ["requested"] = true, ["written"] = false, ["probe"] = observed });
                probe.Dispose(); setupProbe = null; return true;
            }
            if (probe?.ObservedSave == null || probe.ObservedSave.OperationState != SaveInfo.StateType.None) return false;
            var save = probe.ObservedSave; var completion = persistence.LastCompletedSave; var input = request.PersistenceLoad;
            var leaf = Path.Combine(fixtureAuthority.Root, PersistenceSaveAuthorization.OwnerSeedAutoLeaf);
            Require(probe.Complete && completion != null && completion.Sequence == sequence && ReferenceEquals(completion.Requested, probe.ObservedRequestedSave) &&
                ReferenceEquals(completion.Written, save) && save.Type == SaveInfo.SaveType.Auto && save.IsAutoLevelupSave &&
                save.FileName == PersistenceSaveAuthorization.OwnerSeedAutoLeaf && save.FolderName == leaf && completion.Path == leaf &&
                save.IsActuallySaved && save.HasFileOnDisk && save.GameId == input.GameId && save.GameName == input.GameName &&
                save.Area?.AssetGuidThreadSafe == input.Area && fixtureAuthority.AdmittedNativeWriteRequests == sequence &&
                fixtureAuthorization.AuthorizedWriteCount == sequence && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                persistence.SnapshotCount == sequence && persistence.CompletedSaveCount == sequence && setupSlotOwned && NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Setup level-up Auto lacks its exact request, prepared copy, native completion, run-owned leaf or cumulative quota.");
            NativePersistenceIsolation.RequireOwnedWrite(save);
            var file = new FileInfo(save.FolderName);
            Require(file.Exists && file.Length > 0 && (file.Attributes & FileAttributes.ReparsePoint) == 0 && Path.GetDirectoryName(file.FullName) == fixtureAuthority.Root,
                "Setup level-up Auto did not materialize inside the run-owned isolated root.");
            string sha; using (var hash = System.Security.Cryptography.SHA256.Create()) using (var stream = File.OpenRead(file.FullName))
                sha = BitConverter.ToString(hash.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
            setupAutosaves.Add(new JObject { ["sequence"] = sequence, ["riderId"] = rider.UniqueId, ["case"] = "autosave-on", ["phase"] = "setup-levelup-auto",
                ["type"] = save.Type.ToString(), ["autoLevelup"] = save.IsAutoLevelupSave, ["internalName"] = save.Name, ["fileName"] = save.FileName,
                ["preparedLeaf"] = index == 0 ? PersistenceSaveAuthorization.OwnerSeedAutoLeaf : PersistenceSaveAuthorization.OwnerSeedStagingLeaf,
                ["path"] = file.FullName, ["insideRunRoot"] = true, ["length"] = file.Length, ["sha256"] = sha, ["lastWriteTimeUtcTicks"] = file.LastWriteTimeUtc.Ticks,
                ["completionSequence"] = completion.Sequence, ["requestedObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Requested),
                ["writtenObject"] = NativeOrdinaryAcquisitionProbe.Id(completion.Written), ["admittedWrites"] = fixtureAuthority.AdmittedNativeWriteRequests,
                ["authorizedWrites"] = fixtureAuthorization.AuthorizedWriteCount, ["snapshots"] = persistence.SnapshotCount,
                ["completedSaves"] = persistence.CompletedSaveCount, ["frame"] = Time.frameCount, ["completedBeforeBoundary"] = true,
                ["mountedPersistenceProof"] = false, ["probe"] = probe.Capture() });
            setupLedger.Record(rider.UniqueId, "native-levelup-autosave",
                "Kingmaker.Controllers.LevelUpAutoSaveController.HandleLevelUpComplete -> Game.SaveGame(Auto) into the run-owned isolated save root",
                sequence - 1, sequence, 1, file.FullName);
            fixtureAuthority.ConfirmOwnerSeedLevelup(request, Game.Instance, Game.Instance.SaveManager, NativePersistenceIsolation.Project(save));
            probe.Dispose(); setupProbe = null; return true;
        }

        private void RequireOwnerSeedSetupWindow()
        {
            setupLedger.RequireOpen("native ordinary build choice");
            RequireOwnerSeedAuthority();
            Require(!Game.Instance.Player.IsInCombat, "Owner-seed setup entered combat.");
            // The owned projection exists exactly while setup Autos are expected; otherwise the
            // native setting stays unprojected so an off autosave requests nothing.
            Require(setupWritesExpected > 0 ? setupSlotOwned && NativePersistenceIsolation.NativeSlotRotationEnabled : !setupSlotOwned && !NativePersistenceIsolation.NativeSlotRotationEnabled,
                "Owner-seed builder lost or borrowed its native slot projection.");
        }

        private void PublishSetupEvent(JObject row)
        {
            setupEventCount++;
            if (setupEvents.Count < OwnerSeedEventLimit) setupEvents.Add(row);
            ownerSeedSetup["eventCount"] = setupEventCount; ownerSeedSetup["eventsTruncated"] = setupEventCount > OwnerSeedEventLimit;
        }

        private void FinishOwnerSeedRider(int index)
        {
            var rider = index == 0 ? firstRider : secondRider; var row = (JObject)setupRiderRows[index];
            var pet = rider.Descriptor.Pet; var feature = rider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            Require(setupDriver.Complete && pet != null && pet.Blueprint == horses.HorseUnit && ReferenceEquals(pet.Descriptor.Master.Value, rider) &&
                feature != null && feature.Source != null && rider.Descriptor.GetFact(horses.LevelRank) != null && !originalParty.Contains(pet),
                "Native ordinary Ranger build did not create the sourced reciprocal KMC Horse for rider " + (index == 0 ? "A." : "B."));
            var before = (JObject)row["before"]; var after = ProgressionState(rider);
            // Exactly Ranger 4 at exactly the XP-ready level: no extra levels beyond Hunter's Bond.
            Require((int)after["rangerLevel"] == Chunk7OwnerSeedSetupPlan.RequiredRangerLevel && (int)after["characterLevel"] == ReadyLevel((int)after["experience"]) &&
                !LevelUpController.CanLevelUp(rider.Descriptor), "Rider " + (index == 0 ? "A" : "B") + " did not end at exactly Ranger 4 with no pending or extra level.");
            row["after"] = after; row["nativeCommits"] = setupDriver.NativeCommitCount; row["nativeChoices"] = setupDriver.AcceptedChoiceCount;
            row["nativeAutoLevelupOverrides"] = setupDriver.AutoLevelupOverrideCount;
            row["horseId"] = pet.UniqueId; row["horseBlueprint"] = pet.Blueprint.AssetGuid; row["horseMasterId"] = rider.UniqueId;
            row["horseFeatureSourceGuid"] = feature.Source.AssetGuid; row["presentation"] = "fixture-setup"; row["acquisitionProof"] = false;
            setupLedger.Record(rider.UniqueId, "native-ordinary-ranger-levels",
                "Kingmaker.UI.LevelUp.CharacterBuildController.HandleLevelUpStart/SetClass/SetFeature/BuyAttribute/SpendSkillPoint/Next (Chunk7OrdinaryBuildDriver ExistingRangerLevels)",
                (long)before["characterLevel"], (long)after["characterLevel"], setupDriver.NativeCommitCount,
                "Hunter's Bond -> Ranger companion -> KMC Horse " + pet.UniqueId);
            if (index == 0) firstMount = pet; else secondMount = pet;
            nativeScope.Track(pet); setupDriver = null;
        }

        private void ApplyOwnerSeedExperience()
        {
            var assistance = request.OwnedPairs.XpAssistance; var rider = secondRider; var progression = rider.Descriptor.Progression;
            var builder = Game.Instance.UI?.CharacterBuildController;
            Require(builder != null && builder.LevelUpController == null && builder.Unit == null, "A native build session is still open before rider B assistance.");
            var facts = RiderFacts(rider);
            if (assistance == null)
            {
                Require(Chunk7OwnerSeedSetupPlan.EligibleWithoutAssistance(facts), "Rider B became ineligible without its selected assistance.");
                ownerSeedSetup["riderBExperience"] = new JObject { ["assisted"] = false, ["experience"] = progression.Experience };
                return;
            }
            setupLedger.RequireOpen("native XP assistance");
            RequireOwnerSeedAuthority();
            Require(Chunk7OwnerSeedSetupPlan.RiderErrors(facts, "B").Count == 0 && !Chunk7OwnerSeedSetupPlan.EligibleWithoutAssistance(facts) &&
                assistance.UnitId == rider.UniqueId && assistance.TargetCharacterLevel == Chunk7OwnerSeedSetupPlan.MinimalTargetLevel(facts) &&
                facts.ExperienceRate == 100, "Rider B XP assistance no longer matches its exact minimal eligibility plan.");
            Chunk7AssistedFixtureNativeBoundary.RequireMethod(typeof(UnitProgressionData), "GainExperience", 0x06001F80, typeof(void), new[] { typeof(int), typeof(bool) });
            var threshold = BlueprintRoot.Instance.Progression.XPTable.GetBonus(assistance.TargetCharacterLevel);
            var before = progression.Experience; var levelBefore = progression.CharacterLevel; var moneyBefore = Game.Instance.Player.Money;
            Require(threshold > before && threshold - before <= Chunk7AssistedShortfall.MaximumExperienceShortfall, "Rider B XP shortfall is outside the bounded setup ceiling.");
            var delta = threshold - before;
            progression.GainExperience(delta, false);
            Require(progression.Experience == threshold && progression.CharacterLevel == levelBefore && ReadyLevel(progression.Experience) == assistance.TargetCharacterLevel &&
                LevelUpController.CanLevelUp(rider.Descriptor) && Game.Instance.Player.Money == moneyBefore && rider.Descriptor.Pet == null,
                "Native XP assistance changed more than rider B's exact declared experience.");
            setupLedger.Record(rider.UniqueId, "native-xp", "Kingmaker.UnitLogic.UnitProgressionData.GainExperience(int, log: false)", before, progression.Experience, delta,
                "to character level " + assistance.TargetCharacterLevel + " (XP table threshold " + threshold + "), ExperienceRatePercent 100");
            ownerSeedSetup["riderBExperience"] = new JObject { ["assisted"] = true, ["targetCharacterLevel"] = assistance.TargetCharacterLevel,
                ["before"] = before, ["after"] = progression.Experience, ["delta"] = delta, ["threshold"] = threshold,
                ["characterLevelBefore"] = levelBefore, ["characterLevelAfter"] = progression.CharacterLevel,
                ["levelReadyAfter"] = LevelUpController.CanLevelUp(rider.Descriptor), ["readyLevelAfter"] = ReadyLevel(progression.Experience) };
        }

        private void CompleteOwnerSeedSetup()
        {
            RequireOwnerSeedAuthority();
            for (var index = 0; index < 2; index++)
            {
                var rider = index == 0 ? firstRider : secondRider; var mount = index == 0 ? firstMount : secondMount;
                var profile = SupportedMountedProfiles.Resolve(rider, mount);
                string rigError;
                Require(profile != null && profile.Id == SupportedMountedProfiles.Horse.Id && SupportedMountedProfiles.IsSupportedPair(rider, mount) &&
                    MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, profile.RiderPoseProfile, out rigError) &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) && Idle(rider) && Idle(mount),
                    "Owner-seed pair " + (index == 0 ? "A" : "B") + " does not resolve medium-humanoid-horse-v1 with a supported idle reciprocal rig.");
                ((JObject)setupRiderRows[index])["profileId"] = profile.Id; ((JObject)setupRiderRows[index])["rigValidated"] = true;
            }
            Require(world.Registry.ActiveCount == 0 && !controls.HasUnsettledRelationshipTransition, "Owner-seed setup already holds mounted ownership.");
            // Close the write scope exactly once: every selected setup Auto completed, the owned
            // projection released, and no further native write is admissible after the boundary.
            Require(setupProbe == null && !SetupBusy(), "A native level-up observer or write still owns SETUP.");
            fixtureAuthority.CloseOwnerSeedSetupWrites(request, Game.Instance, Game.Instance.SaveManager);
            if (setupSlotOwned)
            {
                Require(NativePersistenceIsolation.NativeSlotRotationEnabled, "The owned slot projection was lost during SETUP.");
                NativePersistenceIsolation.DisableNativeSlotRotation(); setupSlotOwned = false;
            }
            setupWritesAtBoundary = fixtureAuthority.AdmittedNativeWriteRequests;
            Require(setupWritesAtBoundary == setupWritesExpected && setupAutosaves.Count == setupRiderRows.Count,
                "SETUP-COMPLETE observed a native write count that differs from the selected setup Autos.");
            // No money or item effect across the whole setup (the native level-up may only identify items).
            var economy = SetupEconomy();
            Require(JToken.DeepEquals(economy, setupEconomyBefore), "Owner-seed setup changed party money or the party item inventory.");
            var boundaryFrame = Time.frameCount; setupClock.Stop();
            setupBoundaryState = SetupState();
            ownerSeedSetup["assistance"] = new JArray(setupLedger.Items.Select(item => new JObject { ["unitId"] = item.UnitId, ["kind"] = item.Kind,
                ["nativeApi"] = item.NativeApi, ["before"] = item.Before, ["after"] = item.After, ["amount"] = item.Amount, ["detail"] = item.Detail }));
            evidence["setupComplete"] = new JObject { ["boundary"] = "SETUP-COMPLETE", ["frame"] = boundaryFrame,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["assistanceCount"] = setupLedger.Items.Count,
                ["setupSeconds"] = setupClock.Elapsed.TotalSeconds, ["measuredClockRestarted"] = true, ["state"] = setupBoundaryState.DeepClone(),
                ["writesAtBoundary"] = setupWritesAtBoundary, ["writeScopeClosed"] = fixtureAuthority.OwnerSeedWritesClosed, ["economy"] = economy,
                ["horseItems"] = SetupHorseItems() };
            // The boundary is crossed only after its evidence exists, so any earlier failure is labelled SETUP.
            setupLedger.Complete(boundaryFrame);
            Pass(OwnerSeedSetupRow);
            // The measured phase keeps the historical bounded deadlines from this boundary.
            clock.Restart();
        }

        // Observed after the boundary: any setup-state drift refuses cleanup proof.
        private bool OwnerSeedPetsPreserved()
        {
            if (!OwnerSeed || setupBoundaryState == null) return false;
            return ReferenceEquals(firstRider.Descriptor.Pet, firstMount) && ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider) &&
                ReferenceEquals(secondRider.Descriptor.Pet, secondMount) && ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) &&
                firstMount.IsInState && secondMount.IsInState && JToken.DeepEquals(setupBoundaryState, SetupState());
        }

        // Cleanup never removes setup progression or Horses (zero writes; they end with
        // the disposable process). A setup failure may leave the owned native build
        // session open: it is closed only through the builder's own Esc/Yes route.
        private Chunk7NativeBuilderCancellation setupCancellation;
        // Order: the owned build session first (Esc/Yes only), then the probe and slot
        // projection. Cleanup proves itself only after both were observed released.
        private bool OwnerSeedCleanupReady()
        {
            if (setupDriver != null)
            {
                if (setupCancellation == null)
                {
                    if (setupDriver.TryGetOwnedBuildSession(out var builder))
                        setupCancellation = new Chunk7NativeBuilderCancellation(builder, () => nativeScope.RequireCurrent(),
                            () => LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy);
                    else setupDriver = null;
                }
                if (setupCancellation != null)
                {
                    if (!setupCancellation.TryDrain()) return false;
                    setupDriver = null; setupCancellation = null;
                    if (evidence["ownerSeedSetup"] is JObject setup) setup["nativeBuildSessionCancelledByCleanup"] = true;
                }
            }
            return ReleaseSetupWriteCustody() && (!setupLedger.Completed || OwnerSeedPetsPreserved());
        }

        // The full native field roster (characters and pets) after setup.
        private UnitEntityData[] OwnerSeedRoster() => Chunk7NativeFieldRoster.Capture(Game.Instance.Player);

        // A failed SETUP may still own the probe or projection; release them only after the
        // native writer and callbacks settle (the assisted precedent's cleanup order).
        private bool ReleaseSetupWriteCustody()
        {
            // Nothing is released while a build driver or native writer still owns them.
            if (setupDriver != null || SetupBusy() || setupProbe?.InSynchronousCallback == true) return false;
            if (setupProbe != null)
            {
                if (evidence["ownerSeedSetup"] is JObject setup) setup["releasedLevelupProbe"] = setupProbe.Capture();
                setupProbe.Dispose(); setupProbe = null;
            }
            if (setupSlotOwned)
            {
                Require(NativePersistenceIsolation.NativeSlotRotationEnabled, "Owned slot projection changed externally.");
                NativePersistenceIsolation.DisableNativeSlotRotation(); setupSlotOwned = false;
            }
            // Observed, not assumed: no probe owned and no projection left enabled.
            return setupProbe == null && !setupSlotOwned && !NativePersistenceIsolation.NativeSlotRotationEnabled;
        }

        private bool OwnerSeedOwnershipPreserved() => !setupLedger.Completed || OwnerSeedPetsPreserved();

        private void AddOwnerSeedCleanup(JObject cleanup)
        {
            cleanup["setupCompleted"] = setupLedger.Completed;
            cleanup["ownerSeedSetupPetsPreserved"] = setupLedger.Completed && OwnerSeedPetsPreserved();
            cleanup["setupStateUnchangedAfterBoundary"] = setupLedger.Completed && OwnerSeedPetsPreserved();
            cleanup["setupMutationsAfterBoundary"] = setupLedger.RefusedAfterBoundary; cleanup["setupRetainedInMemoryOnly"] = true;
            var writesAfter = setupWritesAtBoundary < 0 ? -1 : fixtureAuthority.AdmittedNativeWriteRequests - setupWritesAtBoundary;
            cleanup["writesAfterBoundary"] = writesAfter; cleanup["unauthorizedWrites"] = fixtureAuthorization.UnauthorizedWriteCount;
            cleanup["slotProjectionReleased"] = !setupSlotOwned && !NativePersistenceIsolation.NativeSlotRotationEnabled;
            if (setupLedger.Completed && (writesAfter != 0 || fixtureAuthorization.UnauthorizedWriteCount != 0))
                Fail("C7-world-cleanup", "A native write followed SETUP-COMPLETE or was unauthorized; only setup level-up Autos are admitted.");
        }

        // Money plus a digest of the shared party inventory and every party character's
        // equipped items, by blueprint and count (identification state is not included).
        // Every shared-inventory row with its holder, plus the fixed characters' equipped slots
        // (Chunk7OwnerSeedEconomyDigest). Identical at both points or the setup is refused.
        private JObject SetupEconomy()
        {
            var player = Game.Instance.Player;
            var characterIds = new HashSet<string>(setupCharacters.Select(actor => actor.UniqueId), StringComparer.Ordinal);
            var shared = player.Inventory.Items.Where(item => item != null).Select(item => new Chunk7OwnerSeedItemRow {
                Blueprint = item.Blueprint?.AssetGuid, Count = item.Count, HolderId = item.HoldingSlot == null ? null : item.HoldingSlot.Owner?.Unit?.UniqueId ?? "unknown-holder" });
            var equipped = setupCharacters.SelectMany(actor => actor.Body.EquipmentSlots.Where(slot => slot?.MaybeItem != null)
                .Select(slot => new Chunk7OwnerSeedItemRow { Blueprint = slot.MaybeItem.Blueprint?.AssetGuid, Count = slot.MaybeItem.Count, HolderId = actor.UniqueId }));
            var rows = Chunk7OwnerSeedEconomyDigest.Rows(shared.ToArray(), equipped.ToArray(), characterIds);
            string sha; using (var hash = System.Security.Cryptography.SHA256.Create())
                sha = BitConverter.ToString(hash.ComputeHash(System.Text.Encoding.UTF8.GetBytes(string.Join("\n", rows)))).Replace("-", "").ToLowerInvariant();
            return new JObject { ["money"] = player.Money, ["itemRows"] = rows.Length, ["inventorySha256"] = sha,
                ["characterIds"] = new JArray(setupCharacters.Select(actor => actor.UniqueId).OrderBy(id => id, StringComparer.Ordinal)) };
        }

        internal const string OwnerSeedHorseItemsNote = "native Horse body/inventory disclosure; not compared; layout not yet observed natively";
        // Disclosure only, never compared: each setup Horse's items read from the Horse itself
        // (its body slots plus its own inventory collection), heldBy from the actual slot owner.
        private JObject SetupHorseItems()
        {
            var shared = Game.Instance.Player.Inventory;
            var horses = new JArray(new[] { firstMount, secondMount }.Select(mount =>
            {
                var items = new List<KeyValuePair<object, JObject>>();
                Action<Kingmaker.Items.ItemEntity, string> add = (item, slot) =>
                {
                    if (item == null || items.Any(pair => ReferenceEquals(pair.Key, item))) return;
                    items.Add(new KeyValuePair<object, JObject>(item, new JObject { ["blueprint"] = item.Blueprint?.AssetGuid, ["slot"] = slot,
                        ["count"] = item.Count, ["heldBy"] = item.HoldingSlot?.Owner?.Unit?.UniqueId ?? mount.UniqueId }));
                };
                foreach (var slot in mount.Body.AllSlots) if (slot?.MaybeItem != null) add(slot.MaybeItem, slot.GetType().Name);
                var own = mount.Descriptor.Inventory;
                if (own != null && !ReferenceEquals(own, shared))
                    foreach (var item in own.Items) add(item, item?.HoldingSlot == null ? "inventory" : item.HoldingSlot.GetType().Name);
                return new JObject { ["horseId"] = mount.UniqueId, ["sharedCollection"] = ReferenceEquals(own, shared),
                    ["items"] = new JArray(items.Select(pair => pair.Value)) };
            }));
            return new JObject { ["horses"] = horses, ["note"] = OwnerSeedHorseItemsNote };
        }

        private JObject SetupState() => new JObject { ["money"] = Game.Instance.Player.Money, ["experienceRatePercent"] = Game.Instance.Player.ExperienceRatePercent,
            ["pairs"] = new JArray(new[] { firstRider, secondRider }.Select(rider => new JObject { ["riderId"] = rider.UniqueId,
                ["progression"] = ProgressionState(rider), ["petId"] = rider.Descriptor.Pet?.UniqueId, ["petObject"] = Id(rider.Descriptor.Pet),
                ["petMasterId"] = rider.Descriptor.Pet?.Descriptor.Master.Value?.UniqueId,
                ["horseFeatureObject"] = Id(rider.Descriptor.GetFact(horses.HorseFeature)), ["rankObject"] = Id(rider.Descriptor.GetFact(horses.LevelRank)) })) };

        private static JObject ProgressionState(UnitEntityData actor)
        {
            var progression = actor.Descriptor.Progression;
            return new JObject { ["characterLevel"] = progression.CharacterLevel, ["experience"] = progression.Experience,
                ["rangerLevel"] = progression.GetClassLevel(RangerClass()),
                ["classes"] = new JArray(progression.Classes.OrderBy(value => value.CharacterClass.AssetGuid, StringComparer.Ordinal)
                    .Select(value => new JObject { ["classGuid"] = value.CharacterClass.AssetGuid, ["level"] = value.Level })) };
        }

        private Chunk7OwnerSeedRiderFacts RiderFacts(UnitEntityData actor)
        {
            var progression = actor.Descriptor.Progression;
            return new Chunk7OwnerSeedRiderFacts { Id = actor.UniqueId,
                InParty = originalParty.Contains(actor) && Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, actor)) == 1,
                Controllable = actor.IsDirectlyControllable, IsPet = actor.Descriptor.IsPet, HasPet = actor.Descriptor.Pet != null,
                HasDualCompanion = actor.Get<UnitPartDualCompanion>() != null, InCombat = actor.IsInCombat, Idle = Idle(actor),
                Polymorphed = actor.GetActivePolymorph() != null, HasHorseRank = actor.Descriptor.GetFact(horses.LevelRank) != null,
                Size = actor.Descriptor.State.Size.ToString(), OriginalSize = actor.Descriptor.OriginalSize.ToString(),
                CharacterLevel = progression.CharacterLevel, RangerLevel = progression.GetClassLevel(RangerClass()),
                Experience = progression.Experience, ReadyLevel = ReadyLevel(progression.Experience),
                ExperienceRate = Game.Instance.Player.ExperienceRatePercent };
        }

        private static JObject FactsRow(string role, Chunk7OwnerSeedRiderFacts facts) => new JObject { ["role"] = role, ["riderId"] = facts.Id,
            ["size"] = facts.Size, ["originalSize"] = facts.OriginalSize, ["polymorphed"] = facts.Polymorphed, ["controllable"] = facts.Controllable,
            ["petless"] = !facts.HasPet, ["characterLevel"] = facts.CharacterLevel, ["rangerLevel"] = facts.RangerLevel,
            ["experience"] = facts.Experience, ["readyLevel"] = facts.ReadyLevel, ["eligibleWithoutAssistance"] = Chunk7OwnerSeedSetupPlan.EligibleWithoutAssistance(facts),
            ["minimalTargetLevel"] = Chunk7OwnerSeedSetupPlan.MinimalTargetLevel(facts) };

        private static BlueprintCharacterClass RangerClass()
        {
            var ranger = ResourcesLibrary.TryGetBlueprint<BlueprintCharacterClass>(Chunk7OrdinaryBuildDriver.RangerClassGuid);
            Require(ranger != null, "Installed stock Ranger class is unavailable.");
            return ranger;
        }

        private static int ReadyLevel(int xp)
        {
            var level = 0;
            while (level < 20 && BlueprintRoot.Instance.Progression.XPTable.GetBonus(level + 1) <= xp) level++;
            return level;
        }
    }
}
