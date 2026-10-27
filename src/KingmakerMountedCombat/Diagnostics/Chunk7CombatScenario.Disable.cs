using System;
using System.Globalization;
using System.Linq;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UnitLogic;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    // Private implementation draft. Only the closed diagnostic selection can
    // invoke the registered UMM callback or the real removal service here.
    internal sealed partial class Chunk7CombatScenario
    {
        private readonly MountedRemovalPreparation removalPreparation;
        private readonly Func<bool, bool> registeredToggle;
        private bool RegisteredDisableCase => request.OwnedPairs?.Case == Chunk7OwnedPairsSelection.OwnerSeedDisableCase;
        private MountedPairOwner<UnitEntityData, GameMountedRelationshipService> disableOwnerA, disableOwnerB;
        private Action<CleanupTrigger> disableCleanupFault;
        private bool disableObserverAttached, disableServicesOff;
        private string disablePhase;
        private int disableFaultCount, disableAcceptedFrame;
        private int disableTraceOffset;
        private readonly JArray disableVisits = new JArray();
        private JObject registeredDisable;

        private void RequireRegisteredDisableScope()
        {
            Require(RegisteredDisableCase && OwnerSeedDerived && !checkpointOnly && SelectedScenario == RiderFirst &&
                removalPreparation != null && registeredToggle != null && inherited?.Bound == true &&
                fixtureAuthority != null && fixtureAuthorization != null && fixtureAuthority.MaximumNativeLoadRequests == 1 &&
                fixtureAuthority.MaximumNativeWriteRequests == 0 && fixtureAuthority.AdmittedNativeLoadRequests == 1 &&
                fixtureAuthority.AdmittedNativeWriteRequests == 0 && fixtureAuthorization.AuthorizedWriteCount == 0 &&
                fixtureAuthorization.AuthorizedLoadCount == 1 && fixtureAuthorization.IsExactPersistenceScope(fixtureAuthority) &&
                !NativePersistenceIsolation.HasPendingWrites && !world.SerializationBusy && !persistence.HasActiveSaveScope &&
                !persistence.HasSaveAdmissionFence && !persistence.LoadInFlight && !persistence.SaveDraining,
                "Registered disable requires its exact one-load/zero-write owner-seed diagnostic authority.");
            nativeScope.RequireCurrent(); inherited.RequirePreserved();
        }

        private void ObserveDisableVisit(GameMountedRelationshipService relationship, CleanupTrigger trigger)
        {
            if (disablePhase == null) return;
            Require(disableVisits.Count < 8 && (ReferenceEquals(relationship, a.Relationship) || ReferenceEquals(relationship, b.Relationship)),
                "Registered disable visited a foreign pair or exceeded its bounded observation.");
            var original = ReferenceEquals(relationship, a.Relationship) ? disableOwnerA : disableOwnerB;
            var owner = relationship.PairOwner;
            Require(owner == null ? ReferenceEquals(original, disableOwnerA) && disablePhase == "retry-disable" &&
                !original.RetainsObligations() && a.Participant.CanReleaseBoundary : ReferenceEquals(owner, original),
                "Registered disable visited a replaced owner or discarded unresolved custody.");
            disableVisits.Add(new JObject { ["phase"] = disablePhase, ["frame"] = Time.frameCount,
                ["trigger"] = trigger.ToString(), ["pairId"] = original.Id.ToString("N"), ["generation"] = original.Generation,
                ["riderId"] = original.RiderId, ["mountId"] = original.MountId,
                ["observedOwnerId"] = owner?.Id.ToString("N"), ["observedGeneration"] = owner == null ? (long?)null : owner.Generation,
                ["relationshipState"] = relationship.State.ToString() });
        }

        private JObject CaptureDisableState(string label)
        {
            var serializer = MountedSaveCodec.CreateSerializer();
            return new JObject { ["world"] = WorldSnapshot(label), ["paused"] = Game.Instance.IsPaused,
                ["turnBased"] = CombatController.IsInTurnBasedCombat(), ["persistenceEnabled"] = persistence.Enabled,
                ["controls"] = JObject.FromObject(controls.CaptureSnapshot(), serializer),
                ["slots"] = JArray.FromObject(controls.CapturePersistentSlots(), serializer),
                ["nativePrepares"] = new JArray(actors.Select(actor => new JObject { ["actorId"] = actor.UniqueId,
                    ["count"] = trace?.GrantCount(actor) ?? 0 })),
                ["unrelatedActors"] = new JArray((originalParty ?? new UnitEntityData[0]).Where(actor => !actors.Contains(actor))
                    .Concat(target == null ? new UnitEntityData[0] : new[] { target }).Distinct().Select(actor =>
                    new JObject { ["native"] = JObject.FromObject(MountedPersistenceService.CaptureActor(actor), serializer),
                        ["prepared"] = actor.CombatState.Prepared, ["inCombat"] = actor.IsInCombat })),
                ["snapshotCount"] = persistence.SnapshotCount, ["nativeWrites"] = fixtureAuthority.AdmittedNativeWriteRequests,
                ["authorizedWrites"] = fixtureAuthorization.AuthorizedWriteCount,
                ["healthyAllocationId"] = trace == null ? (int?)null : trace.ObjectIdentity(AllocationRecord(am)),
                ["pairStates"] = new JArray(CaptureDisablePairState(true), CaptureDisablePairState(false)),
                ["participationA"] = a.Participant.CaptureParticipation() == null ? null : JObject.FromObject(a.Participant.CaptureParticipation(), serializer),
                ["participationB"] = b.Participant.CaptureParticipation() == null ? null : JObject.FromObject(b.Participant.CaptureParticipation(), serializer),
                ["aCanRelease"] = a.Participant.CanReleaseBoundary, ["bCanRelease"] = b.Participant.CanReleaseBoundary };
        }

        private JObject CaptureDisablePairState(bool first)
        {
            var pair = first ? a : b; var original = first ? disableOwnerA : disableOwnerB;
            var current = pair.Relationship.PairOwner; var transition = pair.Relationship.LastTransition;
            return new JObject { ["pairId"] = original.Id.ToString("N"), ["generation"] = original.Generation,
                ["riderId"] = original.RiderId, ["mountId"] = original.MountId,
                ["relationshipState"] = pair.Relationship.State.ToString(), ["noPreparedPairResidue"] = pair.Relationship.Runtime.NoPreparedPairResidue,
                ["commandsRetained"] = pair.Combat.HasRetainedPairCommands, ["unsettledShell"] = controls.HasUnsettledShellFor(pair.Relationship),
                ["currentOwnerId"] = current?.Id.ToString("N"), ["currentGeneration"] = current == null ? (long?)null : current.Generation,
                ["currentOwnerState"] = current?.State.ToString(),
                ["lastTransition"] = transition == null ? null : new JObject { ["succeeded"] = transition.Succeeded,
                    ["state"] = transition.State.ToString(), ["trigger"] = transition.Trigger?.ToString(), ["errors"] = new JArray(transition.Errors),
                    ["movementAuthorityResidual"] = transition.MovementAuthorityResidual, ["presentationResidual"] = transition.PresentationResidual } };
        }

        private void ExercisePermanentHorseRemovalRefusal()
        {
            RequireRegisteredDisableScope();
            Require(!Game.Instance.Player.IsInCombat && actors.All(Chunk7NativeFixture.Idle) && world.Registry.ActiveCount == 2 &&
                world.Registry.Owners.Length == 2 && disableOwnerA == null && !disableObserverAttached,
                "Horse removal refusal belongs to the exact settled two-owner exploration start, once.");
            disableOwnerA = world.Registry.FindActive(ar); disableOwnerB = world.Registry.FindActive(br);
            Require(disableOwnerA != null && disableOwnerB != null && disableOwnerA != disableOwnerB,
                "Horse removal refusal lost an original mounted owner.");
            world.PairDismounting += ObserveDisableVisit; disableObserverAttached = true; disablePhase = "removal-refusal";
            var before = CaptureDisableState("removal-refusal-before");
            var assessments = removalPreparation.AssessmentCount; var refusals = removalPreparation.RefusalCount;
            var saves = removalPreparation.CleanupSaveCount;
            var assessment = removalPreparation.Assess();
            var accepted = removalPreparation.Begin();
            var after = CaptureDisableState("removal-refusal-after");
            var horsesObserved = new JArray();
            foreach (var pair in new[] { new[] { ar, am }, new[] { br, bm } })
            {
                var rider = pair[0]; var mount = pair[1];
                Require(mount.Blueprint?.AssetGuid == HorseCompanionBlueprintService.UnitGuid &&
                    ReferenceEquals(rider.Descriptor.Pet, mount) && ReferenceEquals(mount.Descriptor.Master.Value, rider) &&
                    rider.Descriptor.GetFact(horses.HorseFeature) != null &&
                    assessment.PermanentReferences.Any(value => value.Contains("[" + mount.UniqueId + "]")) &&
                    removalPreparation.LastAssessment.PermanentReferences.Any(value => value.Contains("[" + mount.UniqueId + "]")),
                    "Removal refusal did not identify both real permanent KMC Horses and their native feature custody.");
                horsesObserved.Add(new JObject { ["riderId"] = rider.UniqueId, ["horseId"] = mount.UniqueId,
                    ["unitBlueprintGuid"] = mount.Blueprint.AssetGuid, ["featureBlueprintGuid"] = horses.HorseFeature.AssetGuid,
                    ["riderOwnsFeature"] = true, ["reciprocalPetOwnership"] = true });
            }
            evidence["removalRefusal"] = new JObject { ["contract"] = "owner-seed-permanent-horse-removal-refusal-v1",
                ["assessment"] = new JObject { ["safe"] = assessment.Safe, ["mounted"] = assessment.Mounted,
                    ["world"] = JObject.FromObject(assessment.World, MountedSaveCodec.CreateSerializer()), ["reasons"] = new JArray(assessment.Reasons),
                    ["permanentReferences"] = new JArray(assessment.PermanentReferences) },
                ["beginAccepted"] = accepted, ["state"] = removalPreparation.State.ToString(), ["status"] = removalPreparation.Status,
                ["assessmentDelta"] = removalPreparation.AssessmentCount - assessments,
                ["refusalDelta"] = removalPreparation.RefusalCount - refusals, ["cleanupSaveDelta"] = removalPreparation.CleanupSaveCount - saves,
                ["permanentHorses"] = horsesObserved, ["before"] = before, ["after"] = after, ["dismountVisits"] = disableVisits.DeepClone() };
            Require(!accepted && !assessment.Safe && assessment.World.WorldLoaded && !assessment.World.InspectionFailed &&
                !assessment.World.PartyInCombat && !assessment.World.AnyPartyMemberInCombat && assessment.World.DefaultMode && assessment.World.SaveAllowed &&
                RemovalReadinessPolicy.WorldReasons(assessment.World).Count == 0 &&
                removalPreparation.State == RemovalPreparationState.Refused && removalPreparation.AssessmentCount == assessments + 2 &&
                removalPreparation.RefusalCount == refusals + 1 && removalPreparation.CleanupSaveCount == saves && disableVisits.Count == 0 &&
                JToken.DeepEquals(before["world"]["owners"], after["world"]["owners"]) &&
                JToken.DeepEquals(before["controls"], after["controls"]) && JToken.DeepEquals(before["slots"], after["slots"]),
                "Permanent Horse refusal changed the mounted world, controls or native save boundary.");
            RequireDisableDebt(before, after); disablePhase = null;
            Pass("C7-combat-permanent-horse-removal-refusal");
        }

        private static void RequireDisableDebt(JObject before, JObject after)
        {
            Require(SameBudgets((JObject)before["world"], (JObject)after["world"]) &&
                JToken.DeepEquals(before["nativePrepares"], after["nativePrepares"]) &&
                JToken.DeepEquals(before["unrelatedActors"], after["unrelatedActors"]) &&
                JToken.DeepEquals(before["healthyAllocationId"], after["healthyAllocationId"]) &&
                JToken.DeepEquals(before["turnBased"], after["turnBased"]) &&
                (long)before["world"]["gameTicks"] == (long)after["world"]["gameTicks"] &&
                (int)before["world"]["round"] == (int)after["world"]["round"] &&
                (int)before["snapshotCount"] == 0 && (int)after["snapshotCount"] == 0 &&
                (int)before["nativeWrites"] == 0 && (int)after["nativeWrites"] == 0 &&
                (int)before["authorizedWrites"] == 0 && (int)after["authorizedWrites"] == 0 &&
                ((JArray)before["world"]["actors"]).All(row => JToken.DeepEquals(row["prepared"],
                    ((JArray)after["world"]["actors"]).Single(other => (string)other["id"] == (string)row["id"])["prepared"])),
                "Registered disable/removal changed native actor debt, preparation, clock, round or save counts.");
        }

        private static void RequireNativeDecay(JObject before, JObject after, double elapsed, params string[] axes)
        {
            foreach (var axis in axes)
            {
                var oldValue = NativeNumber(before[axis]); var newValue = NativeNumber(after[axis]);
                Require(oldValue >= 0 && newValue >= 0 && newValue <= oldValue + 0.000001 &&
                    oldValue - newValue <= elapsed + 0.003,
                    "Disabled native frame changed " + axis + " beyond elapsed game time.");
            }
        }

        private static double NativeNumber(JToken value)
        {
            Require(value != null && (value.Type == JTokenType.Integer || value.Type == JTokenType.Float),
                "Native disable resource is not a JSON number.");
            double result;
            Require(double.TryParse(value.ToString(Newtonsoft.Json.Formatting.None), NumberStyles.Float,
                CultureInfo.InvariantCulture, out result) && !double.IsNaN(result) && !double.IsInfinity(result),
                "Native disable resource is not a finite JSON number.");
            return result;
        }

        private void RequireNativeDisableProgression(JObject before, JObject after, JArray transition)
        {
            var ticks = (long)after["world"]["gameTicks"] - (long)before["world"]["gameTicks"];
            Require(ticks > 0 && ticks <= TimeSpan.TicksPerSecond &&
                (int)after["world"]["frame"] > (int)before["world"]["frame"] &&
                (int)after["world"]["round"] == (int)before["world"]["round"] &&
                JToken.DeepEquals(before["nativePrepares"], after["nativePrepares"]) &&
                JToken.DeepEquals(before["healthyAllocationId"], after["healthyAllocationId"]) &&
                (int)after["snapshotCount"] == 0 && (int)after["nativeWrites"] == 0 &&
                (int)after["authorizedWrites"] == 0,
                "Disabled native frame lacks bounded clock, stable native grants or zero-write custody.");
            var elapsed = (double)ticks / TimeSpan.TicksPerSecond;
            var hooks = trace.ObserverHooks.OfType<JObject>().ToArray();
            foreach (var spec in new[] { new[] { "06008355", "Kingmaker.EntitySystem.Entities.UnitEntityData.JoinCombat", "DisableJoinBefore", "DisableJoinAfter" },
                new[] { "0600936F", "Kingmaker.Controllers.Combat.UnitCombatPrepareController.Tick", "DisablePrepareTickBefore", "DisablePrepareTickAfter" },
                new[] { "0600C3BE", "Kingmaker.Controllers.Combat.UnitCombatState+Cooldowns.Clear", "ClearBefore", "ClearAfter" } })
                Require(hooks.Count(h => (string)h["token"] == spec[0] && (string)h["method"] == spec[1] &&
                    (string)h["moduleMvid"] == "07fa1e4d-8618-41b3-9b8d-faa17d3b26f7" &&
                    (string)h["prefix"] == spec[2] && (string)h["postfix"] == spec[3]) == 1,
                    "Disabled native frame lacks an exact installed observation hook.");
            var oldActors = (JArray)before["world"]["actors"];
            var newActors = (JArray)after["world"]["actors"];
            Require(oldActors.Count == 4 && newActors.Count == 4, "Disabled native frame lost exact actors.");
            foreach (var counter in new[] { "TargetSelectionStartCount", "TargetSelectionEndCount", "NativeCastRequestCount",
                "NativeRefusalCount", "DispatchAcceptedCount", "DispatchRejectedCount", "NativePrimaryShellPrepareCount" })
                Require(JToken.DeepEquals(before["controls"][counter], after["controls"][counter]),
                    "Disabled native frame changed a mounted control counter.");
            foreach (var oldActor in oldActors.OfType<JObject>())
            {
                var next = newActors.OfType<JObject>().Single(actor => (string)actor["id"] == (string)oldActor["id"]);
                if ((string)oldActor["id"] == br.UniqueId) continue;
                Require(JToken.DeepEquals(oldActor["prepared"], next["prepared"]) &&
                    JToken.DeepEquals(oldActor["native"]["inCombat"], next["native"]["inCombat"]) &&
                    JToken.DeepEquals(oldActor["reactions"], next["reactions"]) &&
                    JToken.DeepEquals(oldActor["native"]["actorObject"], next["native"]["actorObject"]) &&
                    JToken.DeepEquals(oldActor["native"]["grantSequence"], next["native"]["grantSequence"]) &&
                    JToken.DeepEquals(oldActor["native"]["reactionsPerRound"], next["native"]["reactionsPerRound"]),
                    "Disabled native frame changed another pair actor's preparation or combat membership.");
                RequireNativeDecay(oldActor, next, elapsed, "standard", "move", "swift", "reactionCooldown");
                RequireNativeDecay((JObject)oldActor["native"], (JObject)next["native"], elapsed, "initiativeCooldown");
            }
            var oldB = oldActors.OfType<JObject>().Single(actor => (string)actor["id"] == br.UniqueId);
            var newB = newActors.OfType<JObject>().Single(actor => (string)actor["id"] == br.UniqueId);
            var joinedAndPrepared = !(bool)oldB["native"]["inCombat"] && !(bool)oldB["prepared"] &&
                (bool)newB["native"]["inCombat"] && (bool)newB["prepared"];
            Require(joinedAndPrepared && transition != null,
                "Disabled pending rider did not expose its exact native rejoin/preparation boundary.");
            Require(JToken.DeepEquals(oldB["native"]["actorObject"], newB["native"]["actorObject"]) &&
                JToken.DeepEquals(oldB["reactions"], newB["reactions"]) &&
                JToken.DeepEquals(oldB["native"]["grantSequence"], newB["native"]["grantSequence"]) &&
                JToken.DeepEquals(oldB["native"]["reactionsPerRound"], newB["native"]["reactionsPerRound"]) &&
                (double)newB["native"]["initiativeCooldown"] >= 0 &&
                (double)newB["native"]["initiativeCooldown"] <= 6.0 &&
                (bool)newB["native"]["waitingInitiative"],
                "Disabled pending rider lost native identity/reactions or has an invalid prepared initiative.");
            RequireNativeDecay(oldB, newB, elapsed, "reactionCooldown");
            var bEvents = transition.OfType<JObject>().Where(item => (string)item["state"]?["actor"] == br.UniqueId).ToArray();
            Require(!transition.OfType<JObject>().Any(item =>
                new[] { "prepare-before", "prepare-after" }.Contains((string)item["boundary"]) &&
                actors.Any(actor => actor.UniqueId == (string)item["state"]?["actor"])),
                "Disabled native frame unexpectedly entered TurnController.Prepare.");
            var names = new[] { "disable-join-before", "disable-join-after", "disable-prepare-tick-before",
                "clear-before", "clear-after", "disable-prepare-tick-after" };
            var matched = names.Select(name => bEvents.Where(item => (string)item["boundary"] == name).ToArray()).ToArray();
            Require(matched.All(rows => rows.Length == 1) &&
                matched.Select(rows => (int)rows[0]["sequence"]).SequenceEqual(
                    matched.Select(rows => (int)rows[0]["sequence"]).OrderBy(value => value)) &&
                matched.All(rows => (int)rows[0]["frame"] == (int)matched[0][0]["frame"] &&
                    (int)rows[0]["frame"] >= (int)before["world"]["frame"] &&
                    (int)rows[0]["frame"] <= (int)after["world"]["frame"] &&
                    (long)rows[0]["gameTicks"] > (long)before["world"]["gameTicks"] &&
                    (long)rows[0]["gameTicks"] <= (long)after["world"]["gameTicks"] &&
                    rows[0]["state"]["inCombat"].Type == JTokenType.Boolean &&
                    rows[0]["state"]["prepared"].Type == JTokenType.Boolean &&
                    new[] { "standard", "move", "swift", "initiativeCooldown" }.All(axis =>
                        rows[0]["state"][axis] != null &&
                        (rows[0]["state"][axis].Type == JTokenType.Integer || rows[0]["state"][axis].Type == JTokenType.Float))) &&
                matched.All(rows => JToken.DeepEquals(rows[0]["state"]["actorObject"], oldB["native"]["actorObject"])) &&
                !(bool)matched[0][0]["state"]["inCombat"] && (bool)matched[1][0]["state"]["inCombat"] &&
                matched.Skip(1).All(rows => (bool)rows[0]["state"]["inCombat"]) &&
                matched.Take(3).All(rows => !(bool)rows[0]["state"]["prepared"]) &&
                matched.Skip(3).All(rows => (bool)rows[0]["state"]["prepared"]) &&
                (string)matched[3][0]["detail"] == "disablePrepareDepth=1" &&
                (string)matched[4][0]["detail"] == "disablePrepareDepth=1" &&
                (double)matched[3][0]["state"]["standard"] >= 5.9 && (double)matched[3][0]["state"]["move"] >= 2.9 &&
                (double)matched[3][0]["state"]["swift"] >= 5.9 &&
                (double)matched[4][0]["state"]["standard"] <= 0.001 && (double)matched[4][0]["state"]["move"] <= 0.001 &&
                (double)matched[4][0]["state"]["swift"] >= 0 && (double)matched[4][0]["state"]["swift"] <= 0.001 &&
                (double)matched[4][0]["state"]["move"] >= 0 && (double)matched[4][0]["state"]["standard"] >= 0 &&
                (bool)matched[5][0]["state"]["prepared"] &&
                (double)matched[5][0]["state"]["standard"] >= 0 && (double)matched[5][0]["state"]["standard"] <= 0.001 &&
                (double)matched[5][0]["state"]["move"] >= 0 && (double)matched[5][0]["state"]["move"] <= 0.001 &&
                (double)matched[5][0]["state"]["swift"] >= 0 && (double)matched[5][0]["state"]["swift"] <= 0.001 &&
                (double)matched[5][0]["state"]["initiativeCooldown"] > 0 &&
                (double)matched[5][0]["state"]["initiativeCooldown"] <= 6 &&
                matched[5][0]["state"]["waitingInitiative"].Type == JTokenType.Boolean &&
                (bool)matched[5][0]["state"]["waitingInitiative"],
                "Disabled rider's native rejoin, Prepare.Tick and Cooldowns.Clear were not exactly attributed.");
            for (var index = 1; index < matched.Length; index++)
                Require((long)matched[index][0]["gameTicks"] >= (long)matched[index - 1][0]["gameTicks"],
                    "Disabled native causal clock regressed within one frame.");
            for (var index = 0; index <= 3; index++)
                RequireNativeDecay(oldB, (JObject)matched[index][0]["state"], elapsed,
                    "standard", "move", "swift");
            for (var index = 1; index <= 3; index++)
                RequireNativeDecay((JObject)matched[index - 1][0]["state"], (JObject)matched[index][0]["state"],
                    elapsed, "standard", "move", "swift");
            RequireNativeDecay((JObject)matched[5][0]["state"], (JObject)newB["native"],
                elapsed, "initiativeCooldown");
            foreach (var axis in new[] { "standard", "move", "swift" })
                Require((double)newB[axis] <= 0.001 && (double)oldB[axis] >= (axis == "move" ? 2.9 : 5.9),
                    "Disabled rider's native Prepare.Tick did not explain the cleared spent debt.");
            var oldUnrelated = (JArray)before["unrelatedActors"]; var newUnrelated = (JArray)after["unrelatedActors"];
            Require(oldUnrelated.Count == newUnrelated.Count, "Disabled native frame lost an unrelated actor.");
            foreach (var oldRow in oldUnrelated.OfType<JObject>())
            {
                var next = newUnrelated.OfType<JObject>().Single(row => (string)row["native"]["Id"] == (string)oldRow["native"]["Id"]);
                Require(JToken.DeepEquals(oldRow["prepared"], next["prepared"]) &&
                    JToken.DeepEquals(oldRow["inCombat"], next["inCombat"]) &&
                    JToken.DeepEquals(oldRow["native"]["ReactionsRemaining"], next["native"]["ReactionsRemaining"]) &&
                    JToken.DeepEquals(oldRow["native"]["LastSurpriseTicks"], next["native"]["LastSurpriseTicks"]),
                    "Disabled native frame changed unrelated actor membership or preparation.");
                RequireNativeDecay((JObject)oldRow["native"], (JObject)next["native"], elapsed,
                    "Standard", "Move", "Swift", "Initiative", "Reaction");
            }
        }

        private void RequireDisableVisits(string phase)
        {
            var rows = disableVisits.Where(row => (string)row["phase"] == phase).ToArray();
            Require(rows.Length == 2 && new[] { disableOwnerA, disableOwnerB }.All(owner => rows.Count(row =>
                (string)row["pairId"] == owner.Id.ToString("N") && (long)row["generation"] == owner.Generation &&
                (string)row["riderId"] == owner.RiderId && (string)row["mountId"] == owner.MountId &&
                (string)row["trigger"] == CleanupTrigger.ModDisabled.ToString()) == 1),
                "The registered callback did not visit each original owner exactly once with ModDisabled.");
        }

        private void RequireDisableRetainedOwners()
        {
            Require(ReferenceEquals(b.Relationship.PairOwner, disableOwnerB) &&
                ReferenceEquals(world.Registry.FindOwner(br), disableOwnerB) && ReferenceEquals(world.Registry.FindOwner(bm), disableOwnerB) &&
                disableOwnerB.State == MountedPairOwnershipState.Detached && disableOwnerB.RetainsObligations() &&
                a.Relationship.State == RelationshipState.Unmounted && a.Relationship.Runtime.NoPreparedPairResidue &&
                b.Relationship.State == RelationshipState.Unmounted && b.Relationship.Runtime.NoPreparedPairResidue &&
                !a.Combat.HasRetainedPairCommands && !b.Combat.HasRetainedPairCommands &&
                !controls.HasUnsettledShellFor(a.Relationship) && !controls.HasUnsettledShellFor(b.Relationship),
                "The failed pending owner lost exact custody or the healthy owner retained physical residue.");
            var retained = world.Registry.FindOwner(ar);
            Require(retained == null ? a.Relationship.PairOwner == null && world.Registry.FindOwner(am) == null &&
                !disableOwnerA.RetainsObligations() && a.Participant.CanReleaseBoundary :
                ReferenceEquals(retained, disableOwnerA) && ReferenceEquals(world.Registry.FindOwner(am), disableOwnerA) &&
                ReferenceEquals(a.Relationship.PairOwner, disableOwnerA) &&
                disableOwnerA.State == MountedPairOwnershipState.Detached && disableOwnerA.RetainsObligations(),
                "The healthy owner was replaced, discarded with obligations, or retained without a lawful obligation.");
        }

        private void BeginRegisteredDisableFailure()
        {
            RequireRegisteredDisableScope();
            var aState = a.Participant.CaptureParticipation(); var pending = b.Participant.CaptureParticipation();
            var prelude = new JObject {
                ["observerAttached"] = disableObserverAttached, ["servicesStillOn"] = !disableServicesOff,
                ["aCompleted"] = completedPairs.Contains(0), ["bNotCompleted"] = !completedPairs.Contains(1),
                ["persistenceEnabled"] = persistence.Enabled, ["controlsEnabled"] = controls.CaptureSnapshot().Enabled,
                ["aFinalized"] = aState?.Activation?.Finalized == true,
                ["bPendingOnly"] = pending != null && pending.Activation == null && pending.RiderId == null &&
                    pending.MountId == null && pending.PendingSplitId == bm.UniqueId,
                ["nativeTurnBased"] = CombatController.IsInTurnBasedCombat() && !Game.Instance.IsPaused,
                ["combatBoundary"] = RegisteredDisableCombatBoundary(),
                ["twoActiveOwners"] = world.Registry.ActiveCount == 2 && world.Registry.Owners.Length == 2,
                ["bothMounted"] = a.Relationship.State == RelationshipState.Mounted && b.Relationship.State == RelationshipState.Mounted,
                ["originalOwners"] = ReferenceEquals(a.Relationship.PairOwner, disableOwnerA) &&
                    ReferenceEquals(b.Relationship.PairOwner, disableOwnerB)
            };
            if (prelude.Properties().Any(p => !(bool)p.Value))
                evidence["registeredDisablePreludeFailure"] = new JObject { ["clauses"] = prelude,
                    ["actors"] = new JArray(actors.Select(actor => new JObject { ["id"] = actor?.UniqueId,
                        ["inState"] = actor?.IsInState, ["inGame"] = actor?.IsInGame,
                        ["viewAgentPresent"] = actor?.View?.AgentASP != null,
                        ["conscious"] = actor?.Descriptor?.State?.IsConscious,
                        ["commandsPresent"] = actor?.Commands != null, ["commandsEmpty"] = actor?.Commands?.Empty,
                        ["inCombat"] = actor?.IsInCombat })) };
            Require(prelude.Properties().All(p => (bool)p.Value),
                "Registered disable must start with A finalized and B's real pending-only survivor, both original owners.");
            Require(trace != null, "Registered disable requires its native allocation observer.");
            trace.ObserveDisableNativeActor(br);
            var before = CaptureDisableState("registered-disable-before"); var allocation = AllocationRecord(am);
            Require(allocation != null && trace != null && (int)before["healthyAllocationId"] != 0,
                "Registered disable requires A's existing native allocation observation.");
            disableTraceOffset = trace.EventCount;
            registeredDisable = new JObject { ["contract"] = "owner-seed-registered-disable-local-failure-v1",
                ["callback"] = "UnityModManager.ModEntry.OnToggle", ["injectedPairId"] = disableOwnerB.Id.ToString("N"),
                ["before"] = before, ["visits"] = disableVisits, ["traceSequenceBefore"] = disableTraceOffset };
            evidence["registeredDisable"] = registeredDisable;
            disableCleanupFault = trigger => { if (trigger == CleanupTrigger.ModDisabled) { disableFaultCount++; throw new InvalidOperationException("C7 selected pair B ModDisabled subscriber failure"); } };
            b.Relationship.Dismounting += disableCleanupFault;
            try
            {
                disablePhase = "first-disable";
                var accepted = registeredToggle(false);
                if (accepted || !persistence.Enabled || !controls.CaptureSnapshot().Enabled) disableServicesOff = true;
                var held = CaptureDisableState("registered-disable-refused");
                registeredDisable["firstAccepted"] = accepted; registeredDisable["held"] = held;
                registeredDisable["injectionCount"] = disableFaultCount;
                Require(!accepted && disableFaultCount == 1 && persistence.Enabled && controls.CaptureSnapshot().Enabled &&
                    ReferenceEquals(allocation, AllocationRecord(am)) && a.Relationship.LastTransition?.Succeeded == true &&
                    a.Relationship.LastTransition.State == RelationshipState.Unmounted && a.Relationship.LastTransition.Trigger == CleanupTrigger.ModDisabled &&
                    a.Relationship.LastTransition.Errors.Count == 0 && !a.Relationship.LastTransition.MovementAuthorityResidual &&
                    !a.Relationship.LastTransition.PresentationResidual &&
                    b.Relationship.LastTransition?.Succeeded == false && b.Relationship.LastTransition.Trigger == CleanupTrigger.ModDisabled &&
                    b.Relationship.LastTransition.State == RelationshipState.Unmounted && !b.Relationship.LastTransition.MovementAuthorityResidual &&
                    !b.Relationship.LastTransition.PresentationResidual &&
                    b.Relationship.LastTransition.Errors.Count == 1 &&
                    b.Relationship.LastTransition.Errors[0].Contains("C7 selected pair B ModDisabled subscriber failure"),
                    "The first registered disable did not retain exactly the injected B failure and healthy A allocation.");
                RequireDisableVisits("first-disable"); RequireDisableRetainedOwners(); RequireDisableDebt(before, held);
                Require(JToken.DeepEquals(before["participationB"], held["participationB"]), "Failed disable changed B's pending release.");
            }
            finally
            {
                b.Relationship.Dismounting -= disableCleanupFault; disableCleanupFault = null; disablePhase = null;
            }
            var firstFrame = (int)before["world"]["frame"];
            disablePhase = "retry-disable";
            var retry = registeredToggle(false);
            if (retry || !persistence.Enabled || !controls.CaptureSnapshot().Enabled) disableServicesOff = true;
            var disabled = CaptureDisableState("registered-disable-retry-accepted");
            registeredDisable["retryAccepted"] = retry; registeredDisable["retry"] = disabled;
            registeredDisable["retrySameUpdate"] = firstFrame == Time.frameCount;
            Require(retry && firstFrame == Time.frameCount && disableFaultCount == 1 && !persistence.Enabled &&
                !controls.CaptureSnapshot().Enabled && controls.CaptureSnapshot().ExactFactCount == 0 &&
                controls.CaptureSnapshot().ManagedHotbarSlotCount == 0 && controls.CapturePersistentSlots().Length == 0 &&
                ReferenceEquals(allocation, AllocationRecord(am)) && a.Relationship.LastTransition?.Succeeded == true &&
                a.Relationship.LastTransition.State == RelationshipState.Unmounted && a.Relationship.LastTransition.Trigger == CleanupTrigger.ModDisabled &&
                a.Relationship.LastTransition.Errors.Count == 0 && !a.Relationship.LastTransition.MovementAuthorityResidual &&
                !a.Relationship.LastTransition.PresentationResidual &&
                b.Relationship.LastTransition?.Succeeded == true && b.Relationship.LastTransition.Trigger == CleanupTrigger.ModDisabled &&
                b.Relationship.LastTransition.State == RelationshipState.Unmounted && !b.Relationship.LastTransition.MovementAuthorityResidual &&
                !b.Relationship.LastTransition.PresentationResidual &&
                b.Relationship.LastTransition.Errors.Count == 0,
                "Registered disable retry did not disable in the same Update through its real callback.");
            RequireDisableVisits("retry-disable"); RequireDisableRetainedOwners(); RequireDisableDebt(before, disabled);
            Require(JToken.DeepEquals(before["participationB"], disabled["participationB"]), "Successful disable changed B's pending release.");
            disableAcceptedFrame = Time.frameCount; disablePhase = null;
        }

        private bool RegisteredDisableCombatBoundary()
        {
            return Game.Instance.Player.IsInCombat && actors.Length == 4 &&
                actors.All(actor => actor != null && actor.IsInState && actor.IsInGame &&
                    actor.View?.AgentASP != null && actor.Descriptor.State.IsConscious &&
                    actor.Commands != null && actor.Commands.Empty) &&
                ar.IsInCombat && am.IsInCombat && !br.IsInCombat && bm.IsInCombat;
        }

        private void PollRegisteredDisabledFrame()
        {
            RequireRegisteredDisableScope();
            if (Time.frameCount <= disableAcceptedFrame) return;
            Require(disableServicesOff && CombatController.IsInTurnBasedCombat() && !Game.Instance.IsPaused &&
                !persistence.Enabled && !controls.CaptureSnapshot().Enabled &&
                controls.CaptureSnapshot().ExactFactCount == 0 && controls.CaptureSnapshot().ManagedHotbarSlotCount == 0 &&
                controls.CapturePersistentSlots().Length == 0, "Registered disable did not persist through a later native TB frame.");
            var later = CaptureDisableState("registered-disabled-later-frame"); registeredDisable["disabledLaterFrame"] = later;
            var transition = trace.EventsSince(disableTraceOffset); registeredDisable["nativeTransition"] = transition;
            registeredDisable["traceSequenceAfter"] = trace.EventCount;
            RequireNativeDisableProgression((JObject)registeredDisable["retry"], later, transition);
            RequireDisableRetainedOwners();
            Require(JToken.DeepEquals(registeredDisable["before"]["participationB"], later["participationB"]),
                "Disabled frame dropped or restarted B's real pending survivor.");
            var enabled = registeredToggle(true); if (enabled) disableServicesOff = false;
            registeredDisable["reenableAccepted"] = enabled;
            registeredDisable["reenabled"] = CaptureDisableState("registered-reenabled-before-native-teardown");
            Require(enabled && persistence.Enabled && controls.CaptureSnapshot().Enabled && world.Registry.ActiveCount == 0,
                "Registered re-enable did not restore services without remounting.");
            RequireDisableDebt(later, (JObject)registeredDisable["reenabled"]);
            RequireDisableRetainedOwners(); inherited.RequirePreserved();
            EndRegisteredDisableObservation();
            registeredDisable["complete"] = true;
            Pass("C7-combat-registered-disable-local-failure"); BeginCleanup();
        }

        private void EndRegisteredDisableObservation()
        {
            if (disableCleanupFault != null) { b.Relationship.Dismounting -= disableCleanupFault; disableCleanupFault = null; }
            disablePhase = null;
            if (disableServicesOff)
            {
                Require(registeredToggle != null && registeredToggle(true) && persistence.Enabled && controls.CaptureSnapshot().Enabled,
                    "Failed registered-disable diagnostic retains re-enable custody before encounter cleanup.");
                disableServicesOff = false;
            }
            if (disableObserverAttached) { world.PairDismounting -= ObserveDisableVisit; disableObserverAttached = false; }
            trace?.EndDisableNativeObservation();
        }
    }
}
