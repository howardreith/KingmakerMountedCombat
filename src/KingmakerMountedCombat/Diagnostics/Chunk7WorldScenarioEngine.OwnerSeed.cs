using System;
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
    // Horses are fixture setup, never acquisition proof; nothing is saved.
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        internal const string OwnerSeedSetupRow = "C7-world-owner-seed-setup";
        internal const string OwnerSeedDisclosure = "Fixture setup on a disposable run copy of an owner-supplied seed: each Horse was created by the native ordinary Ranger level-up (Hunter's Bond -> Ranger companion -> KMC Horse) and rider B received only the exact native XP its Ranger 4 eligibility requires. This is not acquisition proof; nothing is saved.";
        private const int OwnerSeedEventLimit = 768;
        private const double OwnerSeedSetupStepSeconds = 240, OwnerSeedSetupTotalSeconds = 420;
        private Chunk7OwnerSeedSetupLedger setupLedger;
        private Chunk7OrdinaryBuildDriver setupDriver;
        private readonly Stopwatch setupClock = new Stopwatch();
        private JObject ownerSeedSetup, setupBoundaryState;
        private JArray setupRiderRows, setupEvents;
        private int setupEventCount;
        private bool OwnerSeed => setupLedger != null;

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
            var a = RiderFacts(firstRider); var b = RiderFacts(secondRider);
            var settings = SettingsRoot.Instance;
            Require(settings?.AutosaveEnabled != null && settings.OnlyOneSave != null, "Native save settings are unavailable.");
            ownerSeedSetup = new JObject { ["contract"] = "owner-seed-fixture-setup-v1", ["disclosure"] = OwnerSeedDisclosure,
                ["seedId"] = selection.SeedId, ["seedSha256"] = selection.SeedSha256, ["fixtureSetupOnly"] = true, ["acquisitionProof"] = false,
                ["producerAncestry"] = false, ["loadedArchiveKind"] = "Missing", ["loadedArchiveCanRestore"] = false,
                ["riderFacts"] = new JArray(FactsRow("A", a), FactsRow("B", b)),
                ["nativeLevelUpAutosave"] = new JObject { ["autosaveEnabled"] = settings.AutosaveEnabled.CurrentValue,
                    ["onlyOneSave"] = settings.OnlyOneSave.CurrentValue, ["settingsWritten"] = false },
                ["riders"] = setupRiderRows, ["events"] = setupEvents };
            evidence["ownerSeedSetup"] = ownerSeedSetup;
            var refusals = Chunk7OwnerSeedSetupPlan.RiderErrors(a, "A").Concat(Chunk7OwnerSeedSetupPlan.RiderErrors(b, "B"))
                .Concat(Chunk7OwnerSeedSetupPlan.AssistanceErrors(selection, a, b)).ToArray();
            ownerSeedSetup["admissionRefusals"] = new JArray(refusals);
            Require(refusals.Length == 0, "Owner-seed SETUP refused before any mutation (setup failure, not a behaviour result): " + string.Join(" ", refusals));
            foreach (var rider in new[] { firstRider, secondRider })
            {
                string rigError;
                Require(MountedRiderPoseAdapter.TryValidateSupportedSurface(rider.View, SupportedMountedProfiles.Horse.RiderPoseProfile, out rigError),
                    "Owner-seed rider rig refused before setup: " + rigError);
            }
            // The final commit of each native build (CharacterBuildController.Commit with no
            // further level ready) makes LevelUpAutoSaveController request an Auto save; chained
            // intermediate commits pass isChargen=true and do not. That is two native write
            // requests for this setup. This kind authorizes zero native writes and never
            // changes the profile setting, so an enabled autosave refuses setup here.
            Require(!settings.AutosaveEnabled.CurrentValue && !settings.OnlyOneSave.CurrentValue,
                "Owner-seed SETUP refused before any mutation: native level-up autosave is enabled (or Ironman), and this kind authorizes zero native writes.");
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
                fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedLoadCount == 1 &&
                fixtureAuthorization.AuthorizedWriteCount == 0 && fixtureAuthorization.UnauthorizedWriteCount == 0 &&
                fixtureAuthorization.UnauthorizedLoadCount == 0 && !NativePersistenceIsolation.HasPendingWrites &&
                Game.Instance?.SaveManager != null && Game.Instance.SaveManager.SavePath == fixtureAuthority.Root &&
                fixtureLoader.State == WorkingFixtureLoadState.LoadedAndVerified &&
                fixtureLoader.WorkingPath == Path.Combine(fixtureAuthority.Root, input.FileName) && fixtureLoader.Descriptor != null &&
                fixtureLoader.Descriptor.FileName == input.FileName && fixtureLoader.Descriptor.GameId == input.GameId &&
                fixtureLoader.Descriptor.Area?.AssetGuidThreadSafe == input.Area,
                "Owner-seed setup lost its exact one-load zero-write isolated authority.");
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
                    Next(step == 50 ? 51 : 53); break;
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
            setupDriver = new Chunk7OrdinaryBuildDriver(pins, rider, horses.HorseFeature, horses.HorseUnit.AssetGuid, RequireOwnerSeedSetupWindow, PublishSetupEvent);
        }

        private void RequireOwnerSeedSetupWindow()
        {
            setupLedger.RequireOpen("native ordinary build choice");
            RequireOwnerSeedAuthority();
            Require(!Game.Instance.Player.IsInCombat, "Owner-seed setup entered combat.");
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
            setupLedger.Complete(Time.frameCount); setupClock.Stop();
            setupBoundaryState = SetupState();
            ownerSeedSetup["assistance"] = new JArray(setupLedger.Items.Select(item => new JObject { ["unitId"] = item.UnitId, ["kind"] = item.Kind,
                ["nativeApi"] = item.NativeApi, ["before"] = item.Before, ["after"] = item.After, ["amount"] = item.Amount, ["detail"] = item.Detail }));
            evidence["setupComplete"] = new JObject { ["boundary"] = "SETUP-COMPLETE", ["frame"] = setupLedger.CompletedFrame,
                ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks, ["assistanceCount"] = setupLedger.Items.Count,
                ["setupSeconds"] = setupClock.Elapsed.TotalSeconds, ["measuredClockRestarted"] = true, ["state"] = setupBoundaryState.DeepClone() };
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
        private bool OwnerSeedCleanupReady()
        {
            if (setupDriver != null)
            {
                if (setupCancellation == null)
                {
                    if (!setupDriver.TryGetOwnedBuildSession(out var builder)) { setupDriver = null; return !setupLedger.Completed || OwnerSeedPetsPreserved(); }
                    setupCancellation = new Chunk7NativeBuilderCancellation(builder, () => nativeScope.RequireCurrent(),
                        () => LoadingProcess.Instance.IsLoadingInProcess || NativePersistenceIsolation.HasPendingWrites || world.SerializationBusy);
                }
                if (!setupCancellation.TryDrain()) return false;
                setupDriver = null; setupCancellation = null;
                if (evidence["ownerSeedSetup"] is JObject setup) setup["nativeBuildSessionCancelledByCleanup"] = true;
            }
            return !setupLedger.Completed || OwnerSeedPetsPreserved();
        }

        // The full native field roster (characters and pets) after setup.
        private UnitEntityData[] OwnerSeedRoster() => Chunk7NativeFieldRoster.Capture(Game.Instance.Player);

        private bool OwnerSeedOwnershipPreserved() => !setupLedger.Completed || OwnerSeedPetsPreserved();

        private void AddOwnerSeedCleanup(JObject cleanup)
        {
            cleanup["setupCompleted"] = setupLedger.Completed;
            cleanup["ownerSeedSetupPetsPreserved"] = setupLedger.Completed && OwnerSeedPetsPreserved();
            cleanup["setupStateUnchangedAfterBoundary"] = setupLedger.Completed && OwnerSeedPetsPreserved();
            cleanup["setupMutationsAfterBoundary"] = setupLedger.RefusedAfterBoundary; cleanup["setupRetainedInMemoryOnly"] = true;
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
