using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Blueprints;
using Kingmaker.Blueprints.Root;
using Kingmaker.Blueprints.Classes;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.Enums;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Class.LevelUp;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        private const string ThirdHorseRiderId = "fd213210-71cd-41b0-b35b-68e1b18e9b43";
        private UnitEntityData thirdRider, thirdMount;
        private Chunk7OrdinaryBuildDriver thirdDriver;
        private Chunk7NativeBuilderCancellation thirdCancellation;
        private Chunk7OwnedPairFixture inheritedThirdHorseCustody;
        private JObject thirdBefore;
        private JArray thirdEvents;
        private bool thirdSourceReleased, thirdSetupCompleted;
        private bool ThirdHorseSetup => request.OwnedPairs?.SmallThirdHorseSetup == true;
        private bool ThirdHorseManual => request.OwnedPairs?.IsSmallThirdHorseSave == true;

        private void BeginThirdHorseSetup()
        {
            Require(ThirdHorseSetup && inherited != null && !thirdSourceReleased && thirdDriver == null &&
                request.PersistenceCase == (ThirdHorseManual ? Chunk7OwnedPairsSelection.OwnerSeedCheckpoint : null) &&
                fixtureAuthority.MatchesOwnedPairsPlan(true, false) &&
                fixtureAuthority.AdmittedNativeLoadRequests == 1 && fixtureAuthority.AdmittedNativeWriteRequests == 0 &&
                fixtureAuthorization.AuthorizedLoadCount == 1 && fixtureAuthorization.AuthorizedWriteCount == 0 &&
                persistence.CompletedSaveCount == 0, "Third-Horse setup lacks exact one-load, zero-write inherited authority.");
            thirdRider = originalParty.SingleOrDefault(actor => actor.UniqueId == ThirdHorseRiderId);
            Require(thirdRider != null && thirdRider.Descriptor.Pet == null && !thirdRider.Descriptor.IsPet &&
                thirdRider.IsDirectlyControllable && thirdRider.IsInState && thirdRider.IsInGame && Idle(thirdRider) &&
                thirdRider.Descriptor.State.Size == Size.Medium && thirdRider.Descriptor.OriginalSize == Size.Medium &&
                thirdRider.GetActivePolymorph() == null && thirdRider.Descriptor.Progression.CharacterLevel == 4 &&
                thirdRider.Descriptor.Progression.Experience == 9000 &&
                thirdRider.Descriptor.Progression.GetClassLevel(RangerClass()) == 0 &&
                !LevelUpController.CanLevelUp(thirdRider.Descriptor) &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, thirdRider)) == 1 &&
                Game.Instance.Player.ExperienceRatePercent == 100 && Game.Instance.Player.PartyCharacters.Count() == 5 &&
                Game.Instance.Player.RemoteCompanions.Count() == 0,
                "The selected batch53 story rider is not the exact petless, XP-short native field actor.");
            RequireThirdHorseOriginals();
            thirdBefore = Chunk7OwnedPairFixture.ArrangementOf(originalParty);
            thirdEvents = new JArray();
            evidence["thirdHorseSetup"] = new JObject {
                ["contract"] = "small-source-third-native-horse-setup-v1",
                ["sourceRunId"] = request.OwnedPairs.SourceRunId,
                ["sourceArchiveSha256"] = request.OwnedPairs.SourceArchiveSha256,
                ["disclosure"] = "Disposable batch53 Small Manual derivative: one story companion receives exact native XP to level 8 and four ordinary Ranger UI commits. This is fixture setup, not unassisted acquisition or six-pair stress.",
                ["fixtureSetupOnly"] = true, ["sixPairQualified"] = false,
                ["saveRequests"] = 0, ["nativeCastRequests"] = 0,
                ["targetRiderId"] = thirdRider.UniqueId, ["before"] = thirdBefore.DeepClone(),
                ["events"] = thirdEvents
            };
            // The unchanged source custody ends before the first XP write. Its
            // exact world scope remains; the derivative has separate assertions.
            inheritedThirdHorseCustody = inherited;
            inherited.Release(); inherited = null; thirdSourceReleased = true;
            var progression = thirdRider.Descriptor.Progression;
            Chunk7AssistedFixtureNativeBoundary.RequireMethod(typeof(UnitProgressionData), "GainExperience", 0x06001F80,
                typeof(void), new[] { typeof(int), typeof(bool) });
            var threshold = BlueprintRoot.Instance.Progression.XPTable.GetBonus(8);
            var beforeXp = progression.Experience;
            var money = Game.Instance.Player.Money;
            Require(threshold == 51000 && threshold - beforeXp == 42000 &&
                Game.Instance.UI?.CharacterBuildController?.LevelUpController == null,
                "Third-Horse native XP target or builder availability changed.");
            progression.GainExperience(42000, false);
            Require(progression.Experience == threshold && progression.CharacterLevel == 4 &&
                ReadyLevel(progression.Experience) == 8 && LevelUpController.CanLevelUp(thirdRider.Descriptor) &&
                Game.Instance.Player.Money == money && thirdRider.Descriptor.Pet == null,
                "Third-Horse bounded XP assistance changed more than the selected rider's native experience.");
            ((JObject)evidence["thirdHorseSetup"])["xpAssistance"] = new JObject {
                ["nativeApi"] = "UnitProgressionData.GainExperience(42000, false)",
                ["before"] = beforeXp, ["after"] = progression.Experience, ["delta"] = 42000,
                ["targetLevel"] = 8, ["characterLevelUnchanged"] = true, ["moneyUnchanged"] = true
            };
            var pins = new Chunk7OrdinaryBuildPins(request.RunId, request.OwnedPairs.SourceArchiveSha256,
                Chunk7OrdinaryBuildMode.ExistingRangerLevels, null, null, null, Gender.Male, false, 4, Size.Medium, true);
            thirdDriver = new Chunk7OrdinaryBuildDriver(pins, thirdRider, horses.HorseFeature,
                horses.HorseUnit.AssetGuid, RequireThirdHorseSetupWindow,
                row => { if (thirdEvents.Count < 768) thirdEvents.Add(row); });
        }

        private void UpdateThirdHorseSetup()
        {
            Require(ThirdHorseSetup && thirdSourceReleased && thirdDriver != null &&
                clock.Elapsed.TotalSeconds <= 600 && clock.Elapsed.TotalSeconds - stepStarted <= 240,
                "Third-Horse native setup exceeded its bounded window.");
            if (step <= 81) RequireThirdHorseSetupWindow();
            if (step == 80)
            {
                if (thirdDriver.Tick() != Chunk7OrdinaryBuildStep.Complete) return;
                thirdMount = thirdRider.Descriptor.Pet;
                Require(thirdDriver.NativeCommitCount == 4 && thirdDriver.AutoLevelupOverrideCount <= 4 &&
                    thirdMount != null && thirdMount.Blueprint == horses.HorseUnit &&
                    ReferenceEquals(thirdMount.Descriptor.Master.Value, thirdRider) &&
                    thirdRider.Descriptor.Progression.CharacterLevel == 8 &&
                    thirdRider.Descriptor.Progression.Experience == 51000 &&
                    thirdRider.Descriptor.Progression.GetClassLevel(RangerClass()) == 4 &&
                    !LevelUpController.CanLevelUp(thirdRider.Descriptor),
                    "Four ordinary native Ranger commitments did not produce the third reciprocal Horse.");
                nativeScope.Track(thirdMount);
                ((JObject)evidence["thirdHorseSetup"])["nativeBuild"] = thirdDriver.CaptureState();
                ((JObject)evidence["thirdHorseSetup"])["nativeCommitCount"] = thirdDriver.NativeCommitCount;
                ((JObject)evidence["thirdHorseSetup"])["autoLevelupOverrideCount"] = thirdDriver.AutoLevelupOverrideCount;
                Next(81); return;
            }
            if (step >= 82) { UpdateThirdHorseManual(); return; }
            Require(step == 81, "Unknown third-Horse setup stage.");
            if (!thirdMount.IsInState || thirdMount.View?.AgentASP == null || !Idle(thirdMount)) return;
            RequireThirdHorseSetupPreserved();
            thirdSetupCompleted = true;
            ((JObject)evidence["thirdHorseSetup"])["after"] = Chunk7OwnedPairFixture.ArrangementOf(originalParty);
            ((JObject)evidence["thirdHorseSetup"])["horseId"] = thirdMount.UniqueId;
            var feature = thirdRider.Descriptor.GetFact(horses.HorseFeature) as Feature;
            var rank = thirdRider.Descriptor.GetFact(horses.LevelRank);
            Require(feature?.Source != null && rank != null && rank.GetRank() > 0,
                "Third-Horse native feature lacks its sourced Ranger selection or rank.");
            ((JObject)evidence["thirdHorseSetup"])["nativeHorse"] = new JObject {
                ["riderPetId"] = thirdRider.Descriptor.Pet.UniqueId,
                ["mountId"] = thirdMount.UniqueId,
                ["mountMasterId"] = thirdMount.Descriptor.Master.Value.UniqueId,
                ["blueprint"] = thirdMount.Blueprint.AssetGuid,
                ["featureGuid"] = horses.HorseFeature.AssetGuid,
                ["sourceGuid"] = feature.Source.AssetGuid,
                ["rank"] = rank.GetRank()
            };
            ((JObject)evidence["thirdHorseSetup"])["fieldActorIds"] = new JArray(
                Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Select(actor => actor.UniqueId));
            Pass("C7-small-third-horse-native-setup");
            if (ThirdHorseManual) Next(82);
            else BeginCleanup();
        }

        private void RequireThirdHorseSetupWindow()
        {
            Require(thirdSourceReleased && fixtureAuthority.MatchesOwnedPairsPlan(true, false) &&
                fixtureAuthority.AdmittedNativeLoadRequests == 1 && fixtureAuthority.AdmittedNativeWriteRequests == 0 &&
                fixtureAuthorization.AuthorizedLoadCount == 1 && fixtureAuthorization.AuthorizedWriteCount == 0 &&
                fixtureAuthorization.UnauthorizedWriteCount == 0 && persistence.CompletedSaveCount == 0 &&
                !Game.Instance.Player.IsInCombat && !world.SerializationBusy,
                "Third-Horse setup lost zero-write authority or entered combat.");
            RequireThirdHorseOriginals();
        }

        private void RequireThirdHorseOriginals()
        {
            if (thirdSourceReleased) inheritedThirdHorseCustody.RequireReleasedThirdHorseSourceCustody();
            Require(ReferenceEquals(firstRider.Descriptor.Pet, firstMount) &&
                ReferenceEquals(firstMount.Descriptor.Master.Value, firstRider) &&
                ReferenceEquals(secondRider.Descriptor.Pet, secondMount) &&
                ReferenceEquals(secondMount.Descriptor.Master.Value, secondRider) &&
                world.Registry.Owners.Length == 0 && !controls.HasUnsettledRelationshipTransition,
                "Third-Horse setup changed either inherited reciprocal pair or mounted ownership.");
            if (thirdBefore == null) return;
            var now = Chunk7OwnedPairFixture.ArrangementOf(originalParty);
            Require(JToken.DeepEquals(thirdBefore["money"], now["money"]) &&
                JToken.DeepEquals(thirdBefore["experienceRatePercent"], now["experienceRatePercent"]),
                "Third-Horse setup changed native funds or XP scaling.");
            var beforeRows = (JArray)thirdBefore["actors"];
            var nowRows = (JArray)now["actors"];
            foreach (var prior in beforeRows.Where(row => (string)row["actorId"] != ThirdHorseRiderId))
                Require(JToken.DeepEquals(prior, nowRows.Single(row => (string)row["actorId"] == (string)prior["actorId"])),
                    "Third-Horse setup changed an unrelated native actor's XP or classes.");
        }

        private void RequireThirdHorseSetupPreserved()
        {
            RequireThirdHorseOriginals();
            Require(thirdSetupCompleted || thirdDriver?.Complete == true, "Third-Horse native build is incomplete.");
            Require(ReferenceEquals(thirdRider.Descriptor.Pet, thirdMount) &&
                ReferenceEquals(thirdMount.Descriptor.Master.Value, thirdRider) &&
                thirdRider.Descriptor.GetFact(horses.HorseFeature) != null &&
                thirdRider.Descriptor.GetFact(horses.LevelRank) != null &&
                Game.Instance.Player.PartyCharacters.Count(value => ReferenceEquals(value.Value, thirdRider)) == 1 &&
                Chunk7NativeFieldRoster.Capture(Game.Instance.Player).Length == 8 &&
                Game.Instance.Player.RemoteCompanions.Count() == 0,
                "Third-Horse setup lost its actual sourced pet or eight-actor native field.");
        }
    }
}
