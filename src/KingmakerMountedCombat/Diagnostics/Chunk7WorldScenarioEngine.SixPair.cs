using System;
using System.Collections.Generic;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7WorldScenarioEngine
    {
        private bool SixPairStress => request.OwnedPairs?.IsSixPairStress == true;
        private int sixCycle, sixIndex, sixSampleGroup, sixSampleWindow;
        private readonly HashSet<int> sixActive = new HashSet<int>();
        private readonly Guid[] sixOwners = new Guid[6];
        private readonly JArray sixCycles = new JArray(), sixSamples = new JArray();
        private JObject sixObservation, sixSampleBefore;
        private RetainedObjectIdentity sixIdentities;
        private object[] sixPins;
        private SixPairAttachmentObservation sixAttachmentObservation;
        private static readonly int[] SixSampleActive = { 0, 1, 2, 6, 0 };
        private UnitEntityData SixRider(int index) => inherited.Actors[index * 2];
        private UnitEntityData SixMount(int index) => inherited.Actors[index * 2 + 1];
        private void BeginSixPairStress()
        {
            Require(SixPairStress && inherited?.Actors.Length == 12 && GameMountedRelationshipService.NativePairCapacity == 6,
                "Six-pair stress requires twelve exact inherited roles and selected capacity six.");
            // b210 r39: the cold-saved placement and its declared waypoints must keep native vision clearance, before any input.
            RequireSixPairHostileClearance();
            // v2: full inherited custody (which hashes the staged source receipt and rebuilds the
            // roster and progression) runs at sampling-window boundaries; measured frames carry
            // only the lightweight scope, ownership, settledness and identity-pin guard.
            sixObservation = new JObject { ["contract"] = "cold-qualified-six-pair-lifecycle-v2",
                ["archiveProducerRunId"] = "20261022-c7b-world184-b184s1-sixthhorse-manual",
                ["coldQualificationRunId"] = Chunk7OwnedPairsSelection.SixPairSourceRun,
                ["fixtureSetupWrites"] = 0, ["nativeSaveRequests"] = 0, ["nativeLoadRequests"] = 1,
                ["requiredCycles"] = 3, ["requiredGroundInputs"] = 6, ["requiredAbilityInputs"] = 36,
                ["requiredWindows"] = 15, ["windowsPerState"] = 3, ["measuredFramesCarryFullCustody"] = false,
                ["performanceAccepted"] = false, ["requestFreeControlsProven"] = false,
                ["identityScopeReleased"] = false, ["cycles"] = sixCycles, ["samples"] = sixSamples };
            evidence["sixPairStress"] = sixObservation;
            CaptureSixSiblingOrder();
            sixAttachmentObservation = new SixPairAttachmentObservation(Enumerable.Range(0, 6).Select(i => SixRider(i).UniqueId), logger);
            sixCycle = 1; sixIndex = 0; Next(1000);
        }
        private void RequireSixOwnership(bool fullCustody = true)
        {
            if (fullCustody) inherited.RequirePreserved();
            Require(world.Registry.ActiveCount == sixActive.Count && world.Registry.Owners.Length == sixActive.Count &&
                !controls.HasUnsettledRelationshipTransition && !world.UnifiedTurn.HasUnsettledPreparation,
                "Six-pair native ownership has unsettled or additional custody.");
            for (var i = 0; i < 6; i++)
            {
                var owner = world.Registry.FindOwner(SixRider(i));
                if (sixActive.Contains(i))
                    Require(owner != null && owner.Id == sixOwners[i] && Mounted(SixRider(i), SixMount(i)) &&
                        ReferenceEquals(world.Registry.FindOwner(SixMount(i)), owner), "Six-pair exact active owner changed at " + i + ".");
                else Require(owner == null && world.Registry.FindOwner(SixMount(i)) == null,
                    "Six-pair detached native custody did not retire at " + i + ".");
            }
        }
        private void UpdateSixPairStress()
        {
            Require(clock.Elapsed.TotalSeconds <= 600 && clock.Elapsed.TotalSeconds - stepStarted <= 60,
                "Bounded six-pair scenario deadline at step " + step + ".");
            // An open sampling window verifies full custody at its own boundaries instead.
            if (measurement == null) inherited.RequirePreserved();
            if (Game.Instance.Player.IsInCombat) RefuseSixPairCombatEntry();
            if (shell != null && controls.HasExactRelationshipProcessBinding(shell)) observedExactProcess = true;
            switch (step)
            {
                case 1000:
                    if (CombatController.IsInTurnBasedCombat() || !Actors().All(Idle)) return;
                    RequireSixOwnership(); Next(1001); break;
                case 1001:
                    if (!Actors().All(Idle)) return;
                    RequireSixOwnership();
                    Input(SixRider(sixIndex), SixMount(sixIndex), controls.MountAbility,
                        "six-cycle-" + sixCycle + "-mount-" + sixIndex, SixRider((sixIndex + 1) % 6));
                    Next(1002); break;
                case 1002:
                    if (!InputSettled()) return;
                    FinishInput();
                    Require(Mounted(SixRider(sixIndex), SixMount(sixIndex)), "Native Mount did not attach exact six-pair role.");
                    sixOwners[sixIndex] = world.Registry.FindActive(SixRider(sixIndex)).Id;
                    sixActive.Add(sixIndex); sixIndex++; RequireSixOwnership();
                    if (sixCycle == 3 && (sixIndex == 1 || sixIndex == 2 || sixIndex == 6))
                    { BeginSixSample(sixIndex == 1 ? 1 : sixIndex == 2 ? 2 : 3); break; }
                    if (sixIndex < 6) { Next(1001); break; }
                    if (sixCycle == 1)
                    { snapshots.Add(CaptureWorld("six-first-native-mounts")); Pass("C7-six-native-mounts"); sixIndex = 0; Next(1010); break; }
                    sixIndex = 0; Next(1003); break;
                case 1010:
                    if (!Actors().All(Idle)) return;
                    RequireSixOwnership();
                    Move(SixRider(sixIndex), SixMount(sixIndex), SixMount(sixIndex).Position + new Vector3(2, 0, 0),
                        SixRider((sixIndex + 1) % 6), "six-ground-" + sixIndex);
                    Next(1011); break;
                case 1011:
                    if (!MovementSettled()) return;
                    RequireSixOwnership(); sixIndex++;
                    if (sixIndex < 6) { Next(1010); break; }
                    Pass("C7-six-selection-routing"); sixIndex = 0; Next(1003); break;
                case 1003:
                    if (!Actors().All(Idle)) return;
                    RequireSixOwnership();
                    Input(SixRider(sixIndex), SixRider(sixIndex), controls.DismountAbility,
                        "six-cycle-" + sixCycle + "-dismount-" + sixIndex, SixRider((sixIndex + 1) % 6));
                    Next(1004); break;
                case 1004:
                    if (!InputSettled() || world.Registry.FindOwner(SixRider(sixIndex)) != null) return;
                    FinishInput(); sixActive.Remove(sixIndex); sixIndex++; RequireSixOwnership();
                    if (sixIndex < 6) { Next(1003); break; }
                    Require(world.BoundaryCount == 6 && world.UnifiedTurn.SnapshotParticipants.Length == 6 &&
                        world.Pairs.All(pair => pair.Relationship.Runtime.NoPreparedPairResidue && PairCommandsSettled(pair)),
                        "Six-pair settled cycle lost its resident services or retained movement/command custody.");
                    RequireSixSiblingOrder();
                    sixCycles.Add(CaptureWorld("six-cycle-" + sixCycle + "-settled"));
                    if (sixCycle == 1) { sixCycle = 2; sixIndex = 0; Next(1001); break; }
                    if (sixCycle == 2) { BeginSixSample(0); break; }
                    Require(inputs.Count == 36 && groundInputs.Count == 6 && sixCycles.Count == 3,
                        "Six-pair lifecycle input/cycle counts differ.");
                    Pass("C7-six-cycles"); BeginSixSample(4); break;
                case 1020:
                    if (!PollSixSample()) return;
                    if (sixSampleGroup == 0) { sixCycle = 3; sixIndex = 0; Next(1001); }
                    else if (sixSampleGroup == 1 || sixSampleGroup == 2) Next(1001);
                    else if (sixSampleGroup == 3) { sixIndex = 0; Next(1003); }
                    else
                    {
                        Require(controls.NativeCastRequestCount == 36 && controls.NativeRefusalCount == 0 &&
                            persistence.SnapshotCount == 0 && persistence.SemanticRestoreCount == 0 && persistence.PresentationRestoreCount == 0,
                            "Six-pair stress performed undeclared native input, save or restoration.");
                        sixObservation["nativeCastRequests"] = controls.NativeCastRequestCount;
                        sixObservation["nativeRefusals"] = controls.NativeRefusalCount;
                        sixObservation["saveSnapshots"] = persistence.SnapshotCount;
                        sixObservation["semanticRestores"] = persistence.SemanticRestoreCount;
                        sixObservation["presentationRestores"] = persistence.PresentationRestoreCount;
                        Pass("C7-six-sampling"); BeginCleanup();
                    }
                    break;
                default: throw new InvalidOperationException("Unknown six-pair step " + step + ".");
            }
        }
        private object[] SixNativePins()
        {
            var game = Game.Instance; var pairs = world.Pairs;
            Require(pairs.Length == 6, "Six-pair resident native boundaries changed.");
            return new object[] { world, game, game.Player, game.State, game.State.LoadedAreaState, game.State.LoadedAreaState.MainState }
                .Concat(pairs.SelectMany(pair => new object[] { pair, pair.Participant }))
                .Concat(Actors().SelectMany(actor => new object[] { actor, actor.Descriptor, actor.View })).ToArray();
        }
        private void RequireSixSample(bool fullCustody)
        {
            RequireSixOwnership(fullCustody);
            Require(sixActive.Count == SixSampleActive[sixSampleGroup] && !Game.Instance.IsPaused &&
                Actors().All(MatchedActorSettled) && NativeCommandsSettled() && world.BoundaryCount == 6 &&
                world.UnifiedTurn.SnapshotParticipants.Length == 6 && world.FailedConstructionCount == 0 &&
                !world.SerializationBusy && !NativePersistenceIsolation.HasPendingWrites &&
                !Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.IsLoadingInProcess &&
                !Kingmaker.EntitySystem.Persistence.LoadingProcess.Instance.QueuedNames.Any() && world.Pairs.All(PairCommandsSettled),
                "Six-pair sampling lost its declared population or settled native custody.");
            var actual = SixNativePins();
            Require(sixPins != null && actual.Length == 54 && sixPins.Length == actual.Length &&
                sixPins.Where((pin, index) => !ReferenceEquals(pin, actual[index])).Count() == 0,
                "Six-pair sampling replaced an exact native identity pin.");
        }
        private JObject CaptureSixSample()
        {
            var value = CaptureWorld("six-matched-boundary");
            foreach (var actor in Actors())
            {
                var row = ((JArray)value["resources"]["actors"]).Single(item => (string)item["id"] == actor.UniqueId);
                row["reactions"] = actor.CombatState.AttackOfOpportunityCount;
                row["reactionAllowance"] = actor.CombatState.AttackOfOpportunityPerRound;
            }
            return new JObject { ["world"] = value, ["identityPins"] = new JArray(sixPins.Select(sixIdentities.Get)),
                ["activePairs"] = sixActive.Count, ["allocationActors"] = world.UnifiedTurn.TrackedAllocationActorCount };
        }
        private void BeginSixSample(int group)
        {
            Require(measurement == null && quietWindow == null, "Six-pair sample overlaps another observer.");
            sixSampleGroup = group; sixSampleWindow = 0;
            if (group == 0)
            {
                Require(sixIdentities == null && sixPins == null, "Six-pair identity scope can be admitted only once.");
                sixPins = SixNativePins(); sixIdentities = new RetainedObjectIdentity(54);
                foreach (var pin in sixPins) sixIdentities.Get(pin);
                Require(sixIdentities.RetainedCount == 54 && sixIdentities.FaultCount == 0,
                    "Six-pair resident identity roles alias or collide.");
            }
            Next(1020);
        }
        private bool PollSixSample()
        {
            // Full custody before the window opens and after it is ready; the measured frames
            // between carry only the lightweight guard, as the two-pair matched pool does.
            RequireSixSample(measurement == null || measurement.Ready);
            if (measurement == null)
            {
                if (!Actors().All(actor => actor.Commands.Empty)) return false;
                sixSampleBefore = CaptureSixSample();
                measurement = new MountedRuntimeMeasurement("six-state-" + sixSampleGroup + "-window-" + sixSampleWindow, world);
                return false;
            }
            if (!measurement.Ready) return false;
            var after = CaptureSixSample(); CompareMatchedPool(sixSampleBefore, after);
            var sample = measurement.Complete(); measurement.Dispose(); measurement = null;
            sixSamples.Add(new JObject { ["group"] = sixSampleGroup, ["window"] = sixSampleWindow,
                ["activePairs"] = sixActive.Count, ["before"] = sixSampleBefore, ["after"] = after, ["sample"] = sample });
            sixSampleBefore = null; sixSampleWindow++;
            return sixSampleWindow == 3;
        }
        private void ReleaseSixSamplePins()
        {
            if (!SixPairStress) return;
            sixAttachmentObservation?.Dispose(); sixAttachmentObservation = null;
            sixIdentities?.Dispose(); sixIdentities = null; sixPins = null;
            if (sixObservation != null) sixObservation["identityScopeReleased"] = true;
        }
    }
}
