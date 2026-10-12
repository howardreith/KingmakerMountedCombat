using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Kingmaker;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.GameModes;
using Kingmaker.UI;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using TurnBased.Controllers;
using UnityEngine;
namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7SmallCalibrationScenario
    {
        private int calibrationStage, attackOffset;
        private MountedPairServices pair;
        private UnitEntityData target;
        private UnitUseAbility shell;
        private UnitMoveTo movement;
        private MountedPairAttackOutcome previousOutcome;
        private Vector3 moveStart;
        private JObject input, move;
        private readonly JArray inputs = new JArray(), poses = new JArray();
        private readonly List<UnitCommand> issued = new List<UnitCommand>();
        private NativeActorAllocationTrace trace;
        private NativeModeTransitionProbe mode;
        private DiagnosticCombatTargetService targetService;
        private ScopedDiagnosticAiLease<UnitEntityData> pairAi;
        private bool BodyMatches()
        {
            var body = NativeSmallRiderBody.Capture(actor, pet);
            RequireBody(body);
            var resolved = SupportedMountedProfiles.Resolve(actor, pet);
            Require(resolved != null && resolved.Id == declaration.Body.ProfileId && resolved.SmallRiderBody != null &&
                declaration.Body.Refusal(body) == null, "Production Small admission does not match the unique compiled body/pose declaration.");
            return true;
        }
        private void AssertAction()
        {
            AssertAuthority();
            Require(!Busy() && !Game.Instance.CutsceneLock && !Game.Instance.IsModeActive(GameModeType.Dialog) &&
                (DialogMessageBox.Instance == null || !DialogMessageBox.Instance.IsShown), "A native load, worker, cutscene or dialog prevents calibration input.");
            Require(!CombatController.IsInTurnBasedCombat(), "This bounded calibration selects real-time native actions only.");
            Require(!settings.EnableUnifiedMountedTurn && !settings.EnablePairedCommandScheduler && !settings.EnableDiagnosticOverlay &&
                settings.EnablePairedActivation && settings.EnableUnsafeMovementExperiment, "Required production paired transport settings differ.");
            BodyMatches(); AssertUnchangedProgression();
        }
        private void BeginCalibration()
        {
            RequireNoOwners(); BodyMatches();
            mode = new NativeModeTransitionProbe(false); mode.DispatchTemporaryValueIfRequired();
            AssertAction();
            evidence["inputs"] = inputs; evidence["poses"] = poses;
            evidence["diagnosticsOffProven"] = false; evidence["humanVisualAcceptance"] = false;
            evidence["nativePreparationManufactured"] = false; evidence["resourceFieldsWritten"] = false;
            evidence["poseQualified"] = false; evidence["breadthQualification"] = "IN PROGRESS";
            poses.Add(new JObject { ["phase"] = "unmounted-field", ["observation"] = Chunk7SmallPoseObservation.Capture(actor, pet, declaration, null) });
            trace = new NativeActorAllocationTrace(actor, pet, world.Primary.Combat, deferInstallation: true);
            trace.Install(); trace.BeginEncounter(request.RunId + ":small-calibration");
            IssueAbility(controls.MountAbility, pet, "native-mount", true); calibrationStage = 1;
        }
        private bool CommandsSettled() => issued.All(value => value.IsFinished && (!(value is UnitUseAbility) ||
            ((UnitUseAbility)value).ExecutionProcess == null || ((UnitUseAbility)value).ExecutionProcess.IsEnded)) &&
            new[] { actor, pet }.All(value => value.Commands.Empty) && !controls.HasUnsettledRelationshipTransition &&
            world.Pairs.All(value => !value.Combat.HasActiveCommand && !value.Combat.HasActiveGroundMovement &&
                !value.Combat.HasChargeOwnership && !controls.HasUnsettledShellFor(value.Relationship));
        private void TickCalibration()
        {
            AssertAction();
            if (targetService?.State == DiagnosticCombatTargetState.Active)
                Require(targetService.RefreshBidirectionalCombatMemoryLease(), "Exact owned target memory/AI lease changed.");
            Game.Instance.IsPaused = false;
            switch (calibrationStage)
            {
                case 1:
                    pair = world.ForActor(actor);
                    if (pair == null || pair.Relationship.State != RelationshipState.Mounted || !CommandsSettled()) return;
                    Require(ReferenceEquals(pair.Relationship.Rider, actor) && ReferenceEquals(pair.Relationship.Mount, pet) &&
                        world.Registry.ActiveCount == 1 && pair.Relationship.Runtime.MountProfileId == declaration.Body.ProfileId,
                        "Native Mount did not commit exactly the declared Small owner.");
                    CompleteRelationshipInput(); ObservePose("mounted-idle"); Pass("C7-small-production-mount");
                    BeginMove(); calibrationStage = 2; break;
                case 2:
                    if (pet.View.MovementAgent.IsReallyMoving && !poses.Children<JObject>().Any(value => (string)value["phase"] == "mounted-moving")) ObservePose("mounted-moving");
                    if (!CommandsSettled()) return;
                    var distance = Vector3.Distance(moveStart, pet.Position);
                    move["after"] = State(); move["actualDistance"] = distance; move["terminal"] = movement != null && movement.IsFinished;
                    Require(distance > .02f && movement != null && movement.Executor == pet && movement.IsFinished &&
                        poses.Children<JObject>().Any(value => (string)value["phase"] == "mounted-moving"), "Native mounted ground input did not visibly advance the exact Horse command.");
                    Require(SameResources((JObject)move["before"], (JObject)move["after"]), "Exploration transport changed native actor resources.");
                    ObservePose("mounted-arrived"); Pass("C7-small-native-ground"); BeginEncounter(); calibrationStage = 3; break;
                case 3:
                    if (!NativeReady(actor) || !NativeReady(pet) || !CommandsSettled()) return;
                    IssuePrimary(false); calibrationStage = 4; break;
                case 4:
                    if (!FinishPrimary(actor)) return;
                    ObservePose("rider-primary-complete"); Pass("C7-small-rider-primary"); calibrationStage = 5; break;
                case 5:
                    if (!NativeReady(pet) || !CommandsSettled()) return;
                    IssuePrimary(true); calibrationStage = 6; break;
                case 6:
                    if (!FinishPrimary(pet)) return;
                    ObservePose("mount-primary-complete"); Pass("C7-small-mount-primary"); calibrationStage = 7; break;
                case 7:
                    if (!CommandsSettled()) return;
                    if (!EndEncounter()) return;
                    IssueAbility(controls.DismountAbility, actor, "native-dismount", true); calibrationStage = 8; break;
                case 8:
                    if (!CommandsSettled() || pair.Relationship.State != RelationshipState.Unmounted || !pair.Relationship.Runtime.NoPreparedPairResidue) return;
                    CompleteRelationshipInput(); Pass("C7-small-native-dismount");
                    poses.Add(new JObject { ["phase"] = "dismounted-field", ["observation"] = Chunk7SmallPoseObservation.Capture(actor, pet, declaration, null) });
                    evidence["representativeNativeActionsObserved"] = true;
                    Pass("C7-small-native-pose-observation"); BeginCleanup(); break;
            }
        }
        private void ObservePose(string phase)
        {
            var row = Chunk7SmallPoseObservation.Capture(actor, pet, declaration, pair.Relationship.Runtime);
            Require(pair.Relationship.Runtime.PoseConfigured && pair.Relationship.Runtime.PoseHealthy &&
                pair.Relationship.Runtime.PoseProfileId == declaration.Pose.Id && pair.Relationship.Runtime.PoseComponentCount == 1,
                "Production Small pose is absent, duplicated or unhealthy.");
            poses.Add(new JObject { ["phase"] = phase, ["observation"] = row });
        }
        private UnitEntityData[] ControlledActors => originalActors.Concat(new[] { actor, pet }).Distinct().ToArray();
        private void CaptureChangedCommands(Dictionary<UnitEntityData, UnitCommand[]> beforeInput)
        {
            foreach (var value in ControlledActors)
                foreach (var command in value.Commands.Raw.Concat(value.Commands.Queue).Where(command => command != null &&
                    !beforeInput[value].Contains(command)).Distinct())
                    if (!issued.Contains(command)) issued.Add(command);
        }
        private void IssueAbility(BlueprintAbility ability, UnitEntityData targetActor, string kind, bool relationship)
        {
            AssertAction(); Require(CommandsSettled(), "Exact native command/process custody must settle before the next ability input.");
            var beforeCommands = ControlledActors.ToDictionary(value => value, value => value.Commands.Raw.Concat(value.Commands.Queue).ToArray());
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(actor); controls.Update();
            input = new JObject { ["kind"] = kind, ["casterId"] = actor.UniqueId, ["targetId"] = targetActor.UniqueId,
                ["abilityGuid"] = ability.AssetGuid, ["before"] = State() }; inputs.Add(input);
            var casts = controls.NativeCastRequestCount; var refusals = controls.NativeRefusalCount;
            var handler = Game.Instance.SelectedAbilityHandler;
            var fact = actor.Descriptor.Abilities.GetAbility(ability)?.Data;
            Require(fact != null && handler != null, "Production native control fact/handler is unavailable.");
            try
            {
                handler.SetAbility(fact);
                try { input["clicked"] = handler.OnClick(targetActor.View.gameObject, targetActor.Position, 0, false, false); }
                finally { handler.DropAbility(); }
            }
            finally { CaptureChangedCommands(beforeCommands); }
            shell = issued.OfType<UnitUseAbility>().SingleOrDefault(value => !beforeCommands[actor].Contains(value) &&
                value.Executor == actor && ReferenceEquals(value.Spell?.Blueprint, ability) && value.Target?.Unit == targetActor);
            input["castDelta"] = controls.NativeCastRequestCount - casts; input["refusalDelta"] = controls.NativeRefusalCount - refusals;
            input["shell"] = trace.ObjectIdentity(shell);
            Require((bool)input["clicked"] && (long)input["castDelta"] == 1 && (long)input["refusalDelta"] == 0 && shell != null &&
                ControlledActors.Where(value => value != actor).All(value => value.Commands.Raw.Concat(value.Commands.Queue).SequenceEqual(beforeCommands[value])),
                "Native ability admission changed the wrong actor or failed exact shell/cast identity.");
            if (relationship)
            {
                var identity = controls.CaptureRelationshipCommandIdentity(shell);
                Require(identity != null && identity.Caster == actor.UniqueId && identity.Target == targetActor.UniqueId, "Exact native relationship context is absent.");
                input["generation"] = identity.Generation; input["control"] = identity.Control;
            }
            Chunk7NativeFixture.Select(originalActors[0]); input["selectionAfterInput"] = originalActors[0].UniqueId;
            Game.Instance.IsPaused = false;
        }
        private void CompleteRelationshipInput()
        {
            input["terminal"] = shell.IsFinished; input["processTerminal"] = shell.ExecutionProcess == null || shell.ExecutionProcess.IsEnded;
            input["after"] = State();
            Require(SameResources((JObject)input["before"], (JObject)input["after"]), "Exploration relationship input changed native actor resources.");
        }
        private void BeginMove()
        {
            AssertAction(); Require(CommandsSettled(), "Mounted movement requires settled exact native commands.");
            Game.Instance.IsPaused = true; Chunk7NativeFixture.Select(actor); Game.Instance.DefaultPointerController.ClearPointerMode();
            moveStart = pet.Position; var destination = moveStart + Vector3.forward * 2f;
            move = new JObject { ["before"] = State(), ["destination"] = new JArray(destination.x, destination.y, destination.z),
                ["casterId"] = actor.UniqueId, ["executorId"] = pet.UniqueId }; evidence["movement"] = move;
            var prior = ControlledActors.ToDictionary(value => value, value => value.Commands.Raw.Concat(value.Commands.Queue).ToArray());
            try { using (var click = new NativeOrdinaryAttackInput(destination)) { click.Predict(); move["clicked"] = click.Click(); } }
            finally { CaptureChangedCommands(prior); }
            movement = pet.Commands.Move as UnitMoveTo; move["command"] = trace.ObjectIdentity(movement);
            Require((bool)move["clicked"] && movement != null && movement.Executor == pet &&
                ControlledActors.Where(value => value != pet).All(value => value.Commands.Raw.Concat(value.Commands.Queue).SequenceEqual(prior[value])), "Ground input did not retain exact mount-only execution.");
            Chunk7NativeFixture.Select(originalActors[0]); move["selectionAfterInput"] = originalActors[0].UniqueId; Game.Instance.IsPaused = false;
        }
        private void BeginEncounter()
        {
            AssertAction(); Require(ControlledActors.All(Chunk7NativeFixture.Idle), "All exact actors must be natively idle before the diagnostic encounter.");
            var ai = typeof(UnitEntityData).GetField("m_AiEnabled", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            Require(ai != null && ai.FieldType == typeof(bool), "Exact native AI backing contract differs.");
            pairAi = new ScopedDiagnosticAiLease<UnitEntityData>(value => value.UniqueId,
                value => (ReferenceEquals(value, actor) || ReferenceEquals(value, pet)) && value.IsInState && value.Group == actor.Group,
                value => value.Commands.Empty, value => (bool)ai.GetValue(value), value => value.IsAIEnabled, (value, enabled) => value.IsAIEnabled = enabled);
            pairAi.Acquire(new[] { actor, pet });
            targetService = new DiagnosticCombatTargetService(logger, true);
            try { target = targetService.Spawn(actor, pet, pet.Position + Vector3.forward * 3.5f, request.RunId + ":small-profile-target", true, true); }
            finally { if (targetService.Target != null) scope.Track(targetService.Target); }
            Require(target != null && targetService.PrepareForPlayerClick(target) && targetService.QueueBidirectionalCombatMemory(actor, target), "Native diagnostic target readiness/memory refused.");
            evidence["targetId"] = target.UniqueId;
            evidence["target"] = new JObject { ["id"] = target.UniqueId, ["object"] = trace.ObjectIdentity(target), ["blueprint"] = target.Blueprint.AssetGuid, ["groupId"] = target.Group.Id,
                ["nativeDiagnosticFixture"] = true, ["playerResourceWrites"] = 0, ["initiativeWrites"] = 0 };
        }
        private static bool NativeReady(UnitEntityData value) => value.IsInCombat && value.CombatState.CanActInCombat && value.CombatState.Prepared &&
            value.Commands.Empty && !value.AreHandsBusyWithAnimation && value.CombatState.Cooldown.StandardAction <= .001f &&
            value.CombatState.Cooldown.MoveAction <= .001f && !Game.Instance.HandsEquipmentController.IsUpdateScheduledFor(value);
        private void IssuePrimary(bool mountAttack)
        {
            Require(targetService.BeginExpectedAttackDispatch(target), "Exact target attack admission refused.");
            previousOutcome = pair.Combat.LastOutcome; attackOffset = trace.EventCount;
            IssueAbility(mountAttack ? controls.MountPrimaryAbility : controls.RiderPrimaryAbility, target,
                mountAttack ? "mount-primary" : "rider-primary", false);
            input["resourceOwnerId"] = (mountAttack ? pet : actor).UniqueId; input["traceOffset"] = attackOffset;
        }
        private bool FinishPrimary(UnitEntityData executor)
        {
            if (!CommandsSettled() || ReferenceEquals(pair.Combat.LastOutcome, previousOutcome)) return false;
            var outcome = pair.Combat.LastOutcome;
            Require(outcome?.Result == "Success" && outcome.ActorId == executor.UniqueId && outcome.ResourceOwnerId == executor.UniqueId &&
                outcome.TargetId == target.UniqueId && outcome.NativeAttackRuleObserved && outcome.NativeCompletedAttackCount == 1,
                "Representative primary did not complete one exact native actor/target attack.");
            var events = trace.EventsSince(attackOffset);
            var costs = events.Children<JObject>().Where(value => (string)value["boundary"] == "cost-after" &&
                (string)value["commandType"] == "KingmakerMountedCombat.Integration.MountedPairAttackCommand").ToArray();
            Require(trace.Complete && costs.Length == 1, "RT primary needs one exact native UnitActionController cost callback.");
            var cost = costs[0];
            var beforeCost = events.Children<JObject>().Where(value => (string)value["boundary"] == "cost-before" &&
                JToken.DeepEquals(value["command"], cost["command"])).ToArray();
            Require(beforeCost.Length == 1 && (int)cost["command"] != 0 &&
                new[] { beforeCost[0], cost }.All(value => (string)value["state"]?["actor"] == executor.UniqueId &&
                    (string)value["commandActor"] == executor.UniqueId && (string)value["commandType"] == "KingmakerMountedCombat.Integration.MountedPairAttackCommand" &&
                    (string)value["actionType"] == "Standard" && (bool)value["acted"] && !(bool)value["nativeTurnBased"]) &&
                (int)cost["sequence"] > (int)beforeCost[0]["sequence"] && (float)cost["state"]["standard"] > (float)beforeCost[0]["state"]["standard"] &&
                new[] { "move", "swift", "reactions", "reactionCooldown" }.All(key => JToken.DeepEquals(beforeCost[0]["state"][key], cost["state"][key])),
                "RT native cost receipt changed actor/action, omitted real spending, or altered another resource.");
            input["after"] = State(); input["terminal"] = true; input["processTerminal"] = true; input["nativeEvents"] = events;
            input["outcome"] = new JObject { ["actorId"] = outcome.ActorId, ["resourceOwnerId"] = outcome.ResourceOwnerId,
                ["targetId"] = outcome.TargetId, ["result"] = outcome.Result, ["nativeAttackRule"] = outcome.NativeAttackRuleObserved,
                ["completedAttacks"] = outcome.NativeCompletedAttackCount, ["singleAttack"] = outcome.SingleAttackMode };
            return true;
        }
        private JObject State() => new JObject { ["frame"] = Time.frameCount, ["gameTicks"] = Game.Instance.TimeController.GameTime.Ticks,
            ["actors"] = new JArray(ControlledActors.Select(value => new JObject { ["id"] = value.UniqueId, ["object"] = trace.ObjectIdentity(value),
                ["standard"] = value.CombatState.Cooldown.StandardAction, ["move"] = value.CombatState.Cooldown.MoveAction,
                ["swift"] = value.CombatState.Cooldown.SwiftAction, ["reactionCooldown"] = value.CombatState.Cooldown.AttackOfOpportunity,
                ["reactions"] = value.CombatState.AttackOfOpportunityCount, ["reactionAllowance"] = value.CombatState.AttackOfOpportunityPerRound,
                ["position"] = new JArray(value.Position.x, value.Position.y, value.Position.z) })),
            ["owners"] = new JArray(world.Registry.Owners.Select(value => new JObject { ["id"] = value.Id.ToString(), ["riderId"] = value.RiderId,
                ["mountId"] = value.MountId, ["generation"] = value.Generation, ["state"] = value.State.ToString() })) };
        private static bool SameResources(JObject left, JObject right) => ((JArray)left["actors"]).Children<JObject>().All(value =>
        {
            var other = ((JArray)right["actors"]).Children<JObject>().Single(row => (string)row["id"] == (string)value["id"]);
            return new[] { "object", "standard", "move", "swift", "reactionCooldown", "reactions", "reactionAllowance" }.All(key => JToken.DeepEquals(value[key], other[key]));
        });
    }
}
