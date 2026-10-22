using System;
using System.Linq;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Newtonsoft.Json.Linq;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ManualDerivedScenario
    {
        private Chunk7NativePartyFieldDeparture sixthDeparture;
        private UnitEntityData sixthDepartureActor;
        private object sixthDepartureDescriptor;
        private JObject beforeSixthDepartureArrangement;
        private int sixthDepartureHireCost;

        private static bool ValidSixthDepartureSource(JObject observations)
        {
            var d = observations?["nativeDeparture"];
            return (string)d?["contract"] == "installed-native-single-field-departure-v1" &&
                (string)d?["actorId"] == Chunk7ManualDerivedSelection.F09DepartureActorId &&
                (string)d?["remoteActorId"] == Chunk7ManualDerivedSelection.F09DepartureActorId &&
                AssistedTrue(d?["nativeDepartureProven"]) && AssistedTrue(d?["closedExactInstance"]) &&
                AssistedFalse(d?["mustBeInParty"]) && AssistedFalse(d?["manualPartyMutation"]) &&
                (int?)d?["openCount"] == 1 && (int?)d?["switchCount"] == 1 && (int?)d?["goCount"] == 1;
        }

        private void StartSixthDeparture()
        {
            Require(selection.UsesF09FiveHorseCold && sixthDeparture == null && actors.Length == 11 &&
                game.Player.PartyCharacters.Count == 6 && game.Player.RemoteCompanions.Count == 0 &&
                game.CurrentMode == GameModeType.Default && !buildStarted,
                "Sixth paid hire requires its unchanged full native source before the declared departure.");
            RequireF09PriorHorses();
            sixthDepartureActor = actors.Single(value => value.UniqueId == Chunk7ManualDerivedSelection.F09DepartureActorId);
            Require(sixthDepartureActor.Descriptor.Pet == null && sixthDepartureActor.Descriptor.Master.Value == null,
                "Only the exact petless native Linzi may leave the field.");
            sixthDepartureDescriptor = sixthDepartureActor.Descriptor;
            beforeSixthDepartureArrangement = CaptureNoArrangement();
            evidence["departureBeforeWorld"] = Snapshot();
            evidence["departureBeforeArrangement"] = beforeSixthDepartureArrangement.DeepClone();
            sixthDeparture = new Chunk7NativePartyFieldDeparture(sixthDepartureActor,
                game.Player.PartyCharacters.Select(value => value.Value.UniqueId).ToArray(), AssertChoiceAuthority, AssertSixthDepartureCleanupAuthority);
            sixthDeparture.Open();
            buildStageStart = clock.Elapsed.TotalSeconds;
        }

        private void TickSixthDeparture()
        {
            Require(clock.Elapsed.TotalSeconds - buildStageStart <= 90, "Declared native departure timed out.");
            if (!sixthDeparture.Tick()) return;
            RequireAuthority(false); RequireF09PriorHorses();
            Require(JToken.DeepEquals(beforeSixthDepartureArrangement, CaptureNoArrangement()),
                "Native departure changed source money, XP or class progression.");
            evidence["nativeDeparture"] = sixthDeparture.Capture();
            evidence["departureAfterWorld"] = Snapshot();
            evidence["departureAfterArrangement"] = CaptureNoArrangement();
            evidence["departureRoster"] = CaptureSixthRoster();
            // Native PartyLevel is an average of the live field characters;
            // removing Linzi can lawfully change the paid-hire price. Preserve
            // the initial six-character source observation separately, then
            // retain the actual native price for the five-character allocation.
            sixthDepartureHireCost = game.Player.GetCustomCompanionCost();
            Require(sixthDepartureHireCost > 0, "Native post-departure hire price is unavailable.");
            evidence["departureReadiness"] = new JObject {
                ["contract"] = "installed-native-post-departure-hire-readiness-v1",
                ["frame"] = UnityEngine.Time.frameCount,
                ["nativePartyLevel"] = game.Player.PartyLevel,
                ["nativeHireBaseCost"] = Kingmaker.Blueprints.Root.BlueprintRoot.Instance.CustomCompanionBaseCost,
                ["nativeHireCost"] = sixthDepartureHireCost, ["money"] = game.Player.Money,
                ["observationOnly"] = true };
            // The native builder is constructed only after this completed
            // selector operation; the six-character source pins remain intact.
            StartBuild();
        }

        private void AssertSixthDepartureCleanupAuthority()
        {
            RequireAuthority(true); RequireNoOwners();
            Require(!Busy() && !game.Player.IsInCombat && !game.CutsceneLock && !game.IsModeActive(GameModeType.Dialog),
                "Owned departure cleanup cannot cross a native worker/combat/world boundary.");
        }

        private bool IsDepartedSourceActor(UnitEntityData actor) => selection.UsesF09FiveHorseCold &&
            sixthDeparture?.CommitRequested == true && ReferenceEquals(actor, sixthDepartureActor);

        private void RequireSixthRoster()
        {
            if (sixthDeparture == null || !sixthDeparture.CommitRequested) return;
            Require(sixthDepartureActor?.UniqueId == Chunk7ManualDerivedSelection.F09DepartureActorId &&
                ReferenceEquals(sixthDepartureActor.Descriptor, sixthDepartureDescriptor) &&
                game.Player.RemoteCompanions.Count(value => ReferenceEquals(value.Value, sixthDepartureActor)) == 1 &&
                !game.Player.PartyCharacters.Any(value => ReferenceEquals(value.Value, sixthDepartureActor)) &&
                game.Player.RemoteCompanions.All(value => ReferenceEquals(value.Value, sixthDepartureActor) ||
                    ReferenceEquals(value.Value, driver?.Actor)) &&
                selection.Readiness.PartyCharacterIds.Where(id => id != sixthDepartureActor.UniqueId)
                    .All(id => game.Player.PartyCharacters.Count(value => value.Value?.UniqueId == id) == 1),
                "Native departure changed more than Linzi's declared field/remote membership.");
        }

        private JObject CaptureSixthRoster()
        {
            Require(selection.UsesF09FiveHorseCold && sixthDeparture?.Completed == true,
                "Sixth-Horse roster observation requires the completed native departure.");
            RequireSixthRoster();
            return new JObject { ["partyCharacterIds"] = new JArray(game.Player.PartyCharacters.Select(value => value.Value.UniqueId)),
                ["remoteCompanionIds"] = new JArray(game.Player.RemoteCompanions.Select(value => value.Value.UniqueId)),
                ["departureActorId"] = sixthDepartureActor.UniqueId, ["departureObject"] = probe.Identity(sixthDepartureActor),
                ["departureDescriptor"] = probe.Identity(sixthDepartureActor.Descriptor),
                ["manualPartyMutation"] = false };
        }
    }
}
