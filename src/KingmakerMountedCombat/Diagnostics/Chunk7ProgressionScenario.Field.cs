using System;
using System.Linq;
using Kingmaker;
using Kingmaker.Controllers.Clicks.Handlers;
using Kingmaker.EntitySystem.Entities;
using Kingmaker.UI.Selection;
using Kingmaker.UnitLogic.Abilities.Blueprints;
using Kingmaker.UnitLogic.Commands;
using Kingmaker.UnitLogic.Commands.Base;
using KingmakerMountedCombat.Domain;
using KingmakerMountedCombat.Integration;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace KingmakerMountedCombat.Diagnostics
{
    internal sealed partial class Chunk7ProgressionScenario
    {
        private int fieldStage;
        private double fieldStageStart;
        private Guid fieldControlOwner;
        private long fieldControlGeneration;
        private Guid fieldMammothOwner;
        private long fieldMammothGeneration;
        private UnitUseAbility fieldShell;
        private UnitCommand fieldMove;
        private Vector3 fieldMoveStart;
        private JObject fieldInput;
        private readonly JArray fieldInputs = new JArray();

        // The paid retrain Manual is admitted by StartCold. This case changes no
        // progression, save, allocation or native resource directly: all three
        // player actions enter through the native input handlers.
        private void TickField()
        {
            Require(selection.IsField && captured && coldInitial != null && elapsed.Elapsed.TotalSeconds <= 300,
                "Medium Mammoth field continuation lost its exact cold source or bounded window.");
            Require(persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0 &&
                !Game.Instance.Player.IsInCombat && actors.All(value => !value.IsInCombat),
                "Medium Mammoth field input acquired combat or native save custody.");
            RequireControlFieldOwner();
            switch (fieldStage)
            {
                case 0:
                    Require(JToken.DeepEquals(coldInitial, CaptureColdState()),
                        "Medium Mammoth field source changed before player input.");
                    if (Time.frameCount < coldFirstFrame + 5) return;
                    evidence["coldContinuation"] = CaptureColdState();
                    evidence["coldContinuationFrame"] = Time.frameCount;
                    evidence["coldRestoration"] = restoration.Capture(); restoration.Dispose(); restoration = null;
                    var control = world.Registry.FindActive(controlRider);
                    Require(control != null && control.Boundary.State == RelationshipState.Mounted &&
                        ReferenceEquals(control.Mount, controlMount) && world.Registry.Owners.Length == 1,
                        "Medium Mammoth field source lost its independent mounted Horse.");
                    fieldControlOwner = control.Id; fieldControlGeneration = control.Generation;
                    evidence["fieldInputs"] = fieldInputs;
                    Pass("C7-progression-cold-continuation");
                    FieldAbilityInput(rider, mount, world.NativeControls.MountAbility, "mount-mammoth");
                    FieldNext(1); break;
                case 1:
                    FieldDeadline();
                    if (!FieldAbilitySettled()) return;
                    var mounted = world.Registry.FindActive(rider);
                    Require(mounted != null && ReferenceEquals(mounted.Mount, mount) &&
                        mounted.Boundary.State == RelationshipState.Mounted &&
                        mounted.Boundary.Runtime.MountProfileId == SupportedMountedProfiles.Mammoth.Id &&
                        world.Registry.Owners.Length == 2 && mounted.Id != fieldControlOwner,
                        "Native player Mount did not establish the exact independent Mammoth profile.");
                    fieldMammothOwner = mounted.Id; fieldMammothGeneration = mounted.Generation;
                    FinishFieldAbility();
                    evidence["fieldMounted"] = FieldState();
                    FieldNext(2); break;
                case 2:
                    FieldDeadline(); RequireMammothFieldOwner();
                    if (Time.frameCount <= (int)evidence["fieldMounted"]["frame"]) return;
                    FieldGroundInput(); FieldNext(3); break;
                case 3:
                    FieldDeadline();
                    RequireMammothFieldOwner();
                    if (fieldMove == null || !fieldMove.IsFinished) return;
                    var delta = mount.Position - fieldMoveStart;
                    var distance = Math.Sqrt(delta.x * delta.x + delta.z * delta.z);
                    Require(distance >= 0.5f && distance <= 5f &&
                        fieldMove.Result == UnitCommand.ResultType.Success &&
                        ReferenceEquals(fieldMove.Executor, mount) && fieldMove.CreatedByPlayer,
                        "Native player ground click did not move the mounted Mammoth within the bounded path.");
                    evidence["fieldMovement"] = new JObject { ["commandFinished"] = true,
                        ["createdByPlayer"] = fieldMove.CreatedByPlayer,
                        ["result"] = fieldMove.Result.ToString(),
                        ["executorId"] = fieldMove.Executor.UniqueId,
                        ["ownerId"] = fieldMammothOwner.ToString("N"),
                        ["ownerGeneration"] = fieldMammothGeneration,
                        ["distance"] = distance, ["startFrame"] = (int?)fieldInput?["frame"],
                        ["endFrame"] = Time.frameCount };
                    FieldNext(4); break;
                case 4:
                    FieldDeadline(); RequireMammothFieldOwner();
                    if (Time.frameCount <= (int)evidence["fieldMovement"]["endFrame"]) return;
                    FieldAbilityInput(rider, rider, world.NativeControls.DismountAbility, "dismount-mammoth");
                    FieldNext(5); break;
                case 5:
                    FieldDeadline();
                    if (!FieldAbilitySettled()) return;
                    // Keep the exact native delivery and pair state even when the
                    // postcondition fails. A terminal shell alone does not prove
                    // that its synchronous Dismount dispatch was accepted.
                    evidence["fieldDismountBoundary"] = CaptureFieldDismountBoundary();
                    Require(world.Registry.FindActive(rider) == null && world.Registry.Owners.Length == 1 &&
                        ReferenceEquals(rider.Descriptor.Pet, mount) &&
                        ReferenceEquals(mount.Descriptor.Master.Value, rider),
                        "Native player Dismount did not release only the Mammoth pair.");
                    FinishFieldAbility();
                    evidence["fieldDismounted"] = FieldState();
                    Require(fieldInputs.Count == 3 && world.NativeControls.NativeCastRequestCount == 2 &&
                        persistence.SnapshotCount == 0 && persistence.CompletedSaveCount == 0,
                        "Mammoth field case issued extra native input or save requests.");
                    Pass("C7-progression-mammoth-field"); BeginCleanup(); break;
                default: throw new InvalidOperationException("Unknown Mammoth field stage.");
            }
        }

        private void FieldNext(int stage) { fieldStage = stage; fieldStageStart = elapsed.Elapsed.TotalSeconds; }
        private void FieldDeadline() => Require(elapsed.Elapsed.TotalSeconds - fieldStageStart <= 45,
            "Native Mammoth field input did not settle within its declared stage window.");
        private void RequireControlFieldOwner()
        {
            if (fieldStage == 0) return;
            var control = world.Registry.FindActive(controlRider);
            Require(control != null && control.Id == fieldControlOwner && control.Generation == fieldControlGeneration &&
                ReferenceEquals(control.Mount, controlMount) && control.Boundary.State == RelationshipState.Mounted &&
                control.Boundary.Runtime.MountProfileId == SupportedMountedProfiles.Horse.Id,
                "Mammoth field input changed the independent mounted Horse owner.");
        }
        private void RequireMammothFieldOwner()
        {
            var mounted = world.Registry.FindActive(rider);
            Require(mounted != null && mounted.Id == fieldMammothOwner &&
                mounted.Generation == fieldMammothGeneration && ReferenceEquals(mounted.Mount, mount) &&
                mounted.Boundary.State == RelationshipState.Mounted &&
                mounted.Boundary.Runtime.MountProfileId == SupportedMountedProfiles.Mammoth.Id &&
                world.Registry.Owners.Length == 2,
                "Mammoth field movement replaced or lost its exact native mounted owner.");
        }
        private void FieldSelect(UnitEntityData actor)
        {
            SelectionManager.Instance.SelectUnit(actor.View, true, true, false);
            Require(SelectionManager.Instance.SelectedUnits.Count == 1 &&
                ReferenceEquals(SelectionManager.Instance.SelectedUnits[0], actor),
                "Mammoth field native selection did not select its exact rider.");
        }
        private void FieldAbilityInput(UnitEntityData caster, UnitEntityData target, BlueprintAbility ability, string label)
        {
            Require(!Game.Instance.IsPaused && fieldShell == null && caster.Commands.Empty && target.View != null,
                "Mammoth field ability input began with unsettled native commands.");
            var fact = caster.Descriptor.Abilities.GetAbility(ability);
            var handler = Game.Instance.SelectedAbilityHandler;
            Require(fact?.Data != null && handler != null, "Mammoth field native ability UI is unavailable.");
            var before = world.NativeControls.NativeCastRequestCount;
            var refused = world.NativeControls.NativeRefusalCount;
            Game.Instance.IsPaused = true;
            try
            {
                FieldSelect(caster); world.NativeControls.Update();
                bool clicked;
                try { handler.SetAbility(fact.Data); clicked = handler.OnClick(target.View.gameObject, target.Position, 0, false, false); }
                finally { try { CaptureCommands(caster); } finally { handler.DropAbility(); } }
                fieldShell = caster.Commands.Raw.OfType<UnitUseAbility>()
                    .SingleOrDefault(value => ReferenceEquals(value.Spell?.Blueprint, ability));
                var identity = world.NativeControls.CaptureRelationshipCommandIdentity(fieldShell);
                fieldInput = new JObject { ["label"] = label, ["frame"] = Time.frameCount,
                    ["casterId"] = caster.UniqueId, ["targetId"] = target.UniqueId,
                    ["abilityGuid"] = ability.AssetGuid, ["clicked"] = clicked,
                    ["nativeCastDelta"] = world.NativeControls.NativeCastRequestCount - before,
                    ["nativeRefusalDelta"] = world.NativeControls.NativeRefusalCount - refused,
                    ["shellControl"] = identity?.Control, ["generationAtInit"] = identity?.Generation };
                fieldInputs.Add(fieldInput);
                Require(clicked && fieldShell != null && identity != null &&
                    identity.Caster == caster.UniqueId && identity.Target == target.UniqueId &&
                    world.NativeControls.NativeCastRequestCount == before + 1 &&
                    world.NativeControls.NativeRefusalCount == refused,
                    "Mammoth field native ability click did not create one exact relationship shell.");
                FieldSelect(controlRider);
            }
            finally { Game.Instance.IsPaused = false; }
        }
        private bool FieldAbilitySettled() => fieldShell != null && fieldShell.IsFinished &&
            (fieldShell.ExecutionProcess == null || fieldShell.ExecutionProcess.IsEnded) &&
            !world.NativeControls.HasUnsettledRelationshipTransition;
        private void FinishFieldAbility()
        {
            Require(FieldAbilitySettled(), "Mammoth field native ability process is still active.");
            fieldInput["shellTerminal"] = true; fieldInput["processTerminal"] = true;
            fieldInput["settledFrame"] = Time.frameCount; fieldShell = null;
        }
        private void FieldGroundInput()
        {
            Require(!Game.Instance.IsPaused && mount.Commands.Empty && rider.Commands.Empty &&
                controlRider.Commands.Empty && controlMount.Commands.Empty,
                "Mammoth field ground input began with unsettled native commands.");
            Game.Instance.IsPaused = true;
            try
            {
                FieldSelect(rider);
                var before = actors.ToDictionary(value => value, value => value.Commands.Move);
                fieldMoveStart = mount.Position;
                var goal = mount.Position + Vector3.right * 2f;
                fieldInput = new JObject { ["label"] = "mammoth-ground", ["frame"] = Time.frameCount,
                    ["selectedId"] = rider.UniqueId, ["executorId"] = mount.UniqueId,
                    ["nativeCallEntered"] = true, ["nativeCallReturned"] = false };
                fieldInputs.Add(fieldInput);
                try { ClickGroundHandler.MoveSelectedUnitsToPoint(goal, false); fieldInput["nativeCallReturned"] = true; }
                finally { foreach (var actor in actors) CaptureCommands(actor); }
                fieldMove = mount.Commands.Move;
                Require(fieldMove != null && !ReferenceEquals(fieldMove, before[mount]) &&
                    ReferenceEquals(fieldMove.Executor, mount) && fieldMove.CreatedByPlayer &&
                    actors.Where(value => !ReferenceEquals(value, mount))
                        .All(value => ReferenceEquals(value.Commands.Move, before[value])),
                    "Mammoth player ground click did not route exactly one native move to its mount.");
                fieldInput["commandValidated"] = true;
                FieldSelect(controlRider);
            }
            finally { Game.Instance.IsPaused = false; }
        }
        private JObject FieldState()
        {
            RequireControlFieldOwner();
            return new JObject { ["frame"] = Time.frameCount,
                ["targetOwnerId"] = world.Registry.FindActive(rider)?.Id.ToString("N"),
                ["targetGeneration"] = world.Registry.FindActive(rider)?.Generation,
                ["targetProfileId"] = world.Registry.FindActive(rider)?.Boundary.Runtime.MountProfileId,
                ["controlOwnerId"] = fieldControlOwner.ToString("N"),
                ["controlGeneration"] = fieldControlGeneration,
                ["owners"] = world.Registry.Owners.Length,
                ["nativeCasts"] = world.NativeControls.NativeCastRequestCount,
                ["saveSnapshots"] = persistence.SnapshotCount,
                ["completedSaves"] = persistence.CompletedSaveCount };
        }

        private JObject CaptureFieldDismountBoundary()
        {
            var target = world.Registry.FindActive(rider);
            var control = world.Registry.FindActive(controlRider);
            return new JObject {
                ["frame"] = Time.frameCount,
                ["shellResult"] = fieldShell.Result.ToString(),
                ["shellFinished"] = fieldShell.IsFinished,
                ["processEnded"] = fieldShell.ExecutionProcess == null || fieldShell.ExecutionProcess.IsEnded,
                ["targetOwnerId"] = target?.Id.ToString("N"),
                ["targetGeneration"] = target?.Generation,
                ["targetState"] = target?.Boundary.State.ToString(),
                ["controlOwnerId"] = control?.Id.ToString("N"),
                ["controlGeneration"] = control?.Generation,
                ["owners"] = world.Registry.Owners.Length,
                ["riderPetId"] = rider.Descriptor.Pet?.UniqueId,
                ["riderPetIsMammoth"] = ReferenceEquals(rider.Descriptor.Pet, mount),
                ["mountMasterId"] = mount.Descriptor.Master.Value?.UniqueId,
                ["mountMasterIsRider"] = ReferenceEquals(mount.Descriptor.Master.Value, rider),
                ["nativeCasts"] = world.NativeControls.NativeCastRequestCount,
                ["nativeRefusals"] = world.NativeControls.NativeRefusalCount,
                ["dispatchAccepted"] = world.NativeControls.DispatchAcceptedCount,
                ["dispatchRejected"] = world.NativeControls.DispatchRejectedCount,
                ["shellLifecycle"] = world.NativeControls.DescribeNativeShellLifecycle()
            };
        }
    }
}
